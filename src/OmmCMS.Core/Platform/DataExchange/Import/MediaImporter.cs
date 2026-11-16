using OmmCMS.Collections;
using OmmCMS.Core.Content.Media;
using OmmCMS.Core.Data;
using OmmCMS.Core.DataExchange.Import.Internal;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Web;
using OmmCMS.Data;
using OmmCMS.Http;
using OmmCMS.IO;
using OmmCMS.Net.Http;

namespace OmmCMS.Core.DataExchange.Import;

public partial class MediaImporter : IMediaImporter
{
    const int MaxCachedDownloadUrls = 2000;

    // Maps downloaded URLs to file names. Avoids redundant downloads.
    // Multiple imported entities can share the same image.
    private readonly Dictionary<string, string> _downloadUrls = new();

    private readonly SmartDbContext _db;
    private readonly IWebHelper _webHelper;
    private readonly IMediaService _mediaService;
    private readonly IFolderService _folderService;
    private readonly DownloadManager _downloadManager;

    public MediaImporter(
        SmartDbContext db,
        IWebHelper webHelper,
        IMediaService mediaService,
        IFolderService folderService,
        DownloadManager downloadManager,
        DataExchangeSettings dataExchangeSettings)
    {
        _db = db;
        _webHelper = webHelper;
        _mediaService = mediaService;
        _folderService = folderService;
        _downloadManager = downloadManager;

        _downloadManager.HttpClient.Timeout = TimeSpan.FromMinutes(dataExchangeSettings.ImageDownloadTimeout);
    }

    public Action<ImportMessage, DownloadManagerItem> MessageHandler { get; set; }

    public virtual DownloadManagerItem CreateDownloadItem(
        IDirectory imageDirectory,
        IDirectory downloadDirectory,
        BaseEntity entity,
        string urlOrPath,
        object state = null,
        int displayOrder = 0,
        HashSet<string> fileNameLookup = null,
        int maxFileNameLength = int.MaxValue)
    {
        Guard.NotNull(imageDirectory);
        Guard.NotNull(downloadDirectory);

        if (urlOrPath.IsEmpty())
        {
            return null;
        }

        var item = new DownloadManagerItem
        {
            Entity = entity,
            Id = displayOrder,
            DisplayOrder = displayOrder,
            State = state
        };

        try
        {
            if (urlOrPath.IsWebUrl())
            {
                // We append quality to avoid importing of image duplicates.
                item.Url = _webHelper.ModifyQueryString(urlOrPath, "q=100");

                if (_downloadUrls.TryGetValue(urlOrPath, out string url))
                {
                    // URL has already been downloaded.
                    item.Success = true;
                    item.FileName = url;
                }
                else
                {
                    item.FileName = GetFileName(WebHelper.GetFileNameFromUrl(urlOrPath), maxFileNameLength, fileNameLookup);

                    fileNameLookup?.Add(item.FileName);
                }

                item.Path = GetAbsolutePath(downloadDirectory, item.FileName);
            }
            else
            {
                item.FileName = GetFileName(Path.GetFileName(urlOrPath), maxFileNameLength);
                item.Success = true;

                item.Path = Path.IsPathRooted(urlOrPath)
                    ? urlOrPath
                    : GetAbsolutePath(imageDirectory, urlOrPath);
            }

            item.MimeType = MimeTypes.MapNameToMimeType(item.FileName);

            return item;
        }
        catch
        {
            InvokeMessageHandler($"Failed to prepare image download for '{urlOrPath}'. Skipping file.", item, ImportMessageType.Error);
            return null;
        }

        static string GetAbsolutePath(IDirectory directory, string fileNameOrRelativePath)
        {
            var path = Path.Combine(directory.PhysicalPath, fileNameOrRelativePath.TrimStart(PathUtility.PathSeparators).Replace('/', '\\'));
            return PathUtility.EnsurePathIsWithinDirectory(path, directory.PhysicalPath);
        }
    }

    #region Generic importers

    public virtual async Task<int> ImportMediaFilesManyAsync(
        DbContextScope scope,
        ICollection<DownloadManagerItem> items,
        MediaFolderNode album,
        Multimap<int, IMediaFile> existingFiles,
        Func<MediaFile, DownloadManagerItem, IMediaFile> assignMediaFileHandler,
        DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
        CancellationToken cancelToken = default)
    {
        Guard.NotNull(scope);
        Guard.NotNull(album);
        Guard.NotNull(existingFiles);
        Guard.NotNull(assignMediaFileHandler);

        var itemsMap = items
            ?.Where(x => x?.Entity != null)
            ?.ToMultimap(x => x.Entity.Id, x => x);

        if (itemsMap.IsNullOrEmpty())
        {
            return 0;
        }

        var newFiles = new Dictionary<string, FileBatchSource>(StringComparer.OrdinalIgnoreCase);
        var downloadedItems = new Dictionary<string, DownloadManagerItem>();

        foreach (var pair in itemsMap)
        {
            try
            {
                var entityId = pair.Key;
                var entityItems = pair.Value;
                var maxDisplayOrder = int.MaxValue;

                // Be kind and assign a continuous DisplayOrder if none has been explicitly specified by the caller.
                if (entityItems.All(x => x.DisplayOrder == 0))
                {
                    maxDisplayOrder = existingFiles.TryGetValues(entityId, out var pmf) && pmf.Count > 0
                        ? pmf.Select(x => x.DisplayOrder).Max()
                        : 0;
                }

                // Download images per entity.
                await Download(entityItems.Where(x => x.Url.HasValue() && !x.Success).ToArray(), downloadedItems, cancelToken);

                foreach (var item in entityItems.OrderBy(x => x.DisplayOrder))
                {
                    if (!Succeeded(item))
                    {
                        continue;
                    }

                    using var file = new MediaImporterFile();
                    if (!file.Init(item, newFiles))
                    {
                        continue;
                    }

                    var currentFiles = existingFiles.ContainsKey(item.Entity.Id)
                        ? existingFiles[item.Entity.Id]
                        : Enumerable.Empty<IMediaFile>();

                    var equalityCheck = await _mediaService.FindEqualFileAsync(file, currentFiles.Select(x => x.MediaFile), true);
                    if (equalityCheck.Success)
                    {
                        // Multiple records may resolve to the same entity during an import.
                        InvokeMessageHandler($"Found equal file in entity data for '{item.FileName}'. Skipping file.", item, reason: ImportMessageReason.EqualFile);
                    }
                    else
                    {
                        if (maxDisplayOrder != int.MaxValue)
                        {
                            item.DisplayOrder = ++maxDisplayOrder;
                        }

                        equalityCheck = await _mediaService.FindEqualFileAsync(file, item.FileName, album.Id, true);
                        if (equalityCheck.Success)
                        {
                            // Reuse an existing file in the album when another record references it.
                            var assignedFile = assignMediaFileHandler(equalityCheck.Value, item);
                            existingFiles.Add(item.Entity.Id, assignedFile);
                            InvokeMessageHandler($"Found equal file in {album.Name} album for '{item.FileName}'. Assigning existing file instead.", item, reason: ImportMessageReason.EqualFileInAlbum);
                        }
                        else
                        {
                            file.Add(item, newFiles);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InvokeMessageHandler(ex.ToAllMessages(), null, ImportMessageType.Warning);
            }
        }

        if (newFiles.Count > 0)
        {
            var postProcessingEnabled = _mediaService.ImagePostProcessingEnabled;

            try
            {
                // Always turn image post-processing off during imports. It can heavily decrease processing time.
                _mediaService.ImagePostProcessingEnabled = false;

                var batchResult = await _mediaService.BatchSaveFilesAsync(newFiles.Values.ToArray(), album, false, duplicateFileHandling, cancelToken);

                foreach (var fileResult in batchResult)
                {
                    if (fileResult.Exception == null && fileResult.File?.Id > 0)
                    {
                        var assignedItems = fileResult.Source.State as List<DownloadManagerItem>;
                        foreach (var item in assignedItems)
                        {
                            var assignedFile = assignMediaFileHandler(fileResult.File.File, item);
                            existingFiles.Add(item.Entity.Id, assignedFile);
                        }
                    }
                }
            }
            finally
            {
                _mediaService.ImagePostProcessingEnabled = postProcessingEnabled;
            }
        }

        await scope.CommitAsync(cancelToken);

        return newFiles.Count;
    }

    public virtual async Task<int> ImportMediaFilesAsync<T>(
        DbContextScope scope,
        ICollection<DownloadManagerItem> items,
        MediaFolderNode album,
        Action<DownloadManagerItem, int> assignMediaFileHandler,
        Func<DownloadManagerItem, Stream, Task<bool>> checkAssignedMediaFileHandler,
        bool checkExistingFile,
        DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
        CancellationToken cancelToken = default) where T : BaseEntity
    {
        Guard.NotNull(scope);
        Guard.NotNull(album);
        Guard.NotNull(assignMediaFileHandler);
        Guard.NotNull(checkAssignedMediaFileHandler);

        items = items?.Where(x => x != null)?.ToArray();
        if (items.IsNullOrEmpty())
        {
            return 0;
        }

        var newFiles = new Dictionary<string, FileBatchSource>(StringComparer.OrdinalIgnoreCase);
        var downloadedItems = new Dictionary<string, DownloadManagerItem>();

        foreach (var item in items)
        {
            try
            {
                var entity = item.Entity as T;

                if (item.Url.HasValue() && !item.Success)
                {
                    await Download([item], downloadedItems, cancelToken);
                }

                if (!Succeeded(item))
                {
                    continue;
                }

                using var file = new MediaImporterFile();
                if (!file.Init(item, newFiles))
                {
                    continue;
                }

                // Check for already assigned files.
                if (await checkAssignedMediaFileHandler(item, file))
                {
                    InvokeMessageHandler($"Found equal file for {nameof(entity)} '{item.FileName}'. Skipping file.", item, reason: ImportMessageReason.EqualFile);
                    continue;
                }

                if (checkExistingFile)
                {
                    var equalityCheck = await _mediaService.FindEqualFileAsync(file, item.FileName, album.Id, true);
                    if (equalityCheck.Success)
                    {
                        assignMediaFileHandler(item, equalityCheck.Value.Id);
                        InvokeMessageHandler($"Found equal file in {album.Name} album for '{item.FileName}'. Assigning existing file instead.", item, reason: ImportMessageReason.EqualFileInAlbum);
                        continue;
                    }
                }

                file.Add(item, newFiles);
            }
            catch (Exception ex)
            {
                InvokeMessageHandler(ex.ToAllMessages(), null, ImportMessageType.Warning);
            }
        }

        if (newFiles.Count > 0)
        {
            var postProcessingEnabled = _mediaService.ImagePostProcessingEnabled;

            try
            {
                // Always turn image post-processing off during imports. It can heavily decrease processing time.
                _mediaService.ImagePostProcessingEnabled = false;

                var batchResult = await _mediaService.BatchSaveFilesAsync(newFiles.Values.ToArray(), album, false, duplicateFileHandling, cancelToken);

                foreach (var fileResult in batchResult)
                {
                    if (fileResult.Exception == null && fileResult.File?.Id > 0)
                    {
                        // Assign MediaFile to corresponding entity via callback.
                        var assignedItems = fileResult.Source.State as List<DownloadManagerItem>;
                        foreach (var item in assignedItems)
                        {
                            assignMediaFileHandler(item, fileResult.File.Id);
                        }
                    }
                }
            }
            finally
            {
                _mediaService.ImagePostProcessingEnabled = postProcessingEnabled;
            }
        }

        await scope.CommitAsync(cancelToken);

        return newFiles.Count;
    }

    #endregion

    #region Specific importers

    public async Task<int> ImportCustomerAvatarsAsync(
        DbContextScope scope,
        ICollection<DownloadManagerItem> items,
        DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
        CancellationToken cancelToken = default)
    {
        return await ImportMediaFilesAsync<Customer>(
            scope,
            items,
            _folderService.GetNodeByPath(SystemAlbumProvider.Customers).Value,
            AddCustomerAvatarMediaFile,
            CheckAssignedFile,
            false,
            duplicateFileHandling,
            cancelToken);

        async Task<bool> CheckAssignedFile(DownloadManagerItem item, Stream stream)
        {
            var customer = (Customer)item.Entity;
            var file = await _mediaService.GetFileByIdAsync(customer.GenericAttributes.AvatarPictureId ?? 0, MediaLoadFlags.AsNoTracking);
            if (file != null)
            {
                var isEqualData = await _mediaService.FindEqualFileAsync(stream, new[] { file.File }, true);
                if (isEqualData.Success)
                {
                    return true;
                }
            }

            return false;
        }

        static void AddCustomerAvatarMediaFile(DownloadManagerItem item, int fileId)
        {
            ((Customer)item.Entity).GenericAttributes.Set(SystemCustomerAttributeNames.AvatarPictureId, fileId);
        }
    }

    #endregion

    #region Utilities

    private async Task Download(DownloadManagerItem[] items, Dictionary<string, DownloadManagerItem> downloadedItems, CancellationToken cancelToken)
    {
        if (items.Length == 0)
        {
            return;
        }

        // Exclude items that have already been downloaded within the current batch.
        // Avoids possible IOException when DownloadManager accesses the file.
        if (downloadedItems.Count > 0 && items.Any(x => !x.Success && downloadedItems.ContainsKey(x.Url)))
        {
            foreach (var item in items)
            {
                if (!item.Success && downloadedItems.TryGetValue(item.Url, out var downloadedItem))
                {
                    item.Success = downloadedItem.Success;
                    item.FileName = downloadedItem.FileName;
                    item.Path = downloadedItem.Path;
                }
            }

            items = items.Where(x => !x.Success).ToArray();
            if (items.Length == 0)
            {
                return;
            }
        }

        // TODO: (mg) (core) Make this fire&forget somehow and sync later.
        // RE: I do not see any extra benefit in this specific case.

        //$"- download {string.Join(',', items.Select(x => x.Url))}".Dump();
        await _downloadManager.DownloadFilesAsync(items, cancelToken);

        foreach (var item in items)
        {
            if (item.Success && File.Exists(item.Path))
            {
                // Cache URL to avoid redundant downloads over multiple batches.
                if (!_downloadUrls.ContainsKey(item.Url))
                {
                    if (_downloadUrls.Count >= MaxCachedDownloadUrls)
                    {
                        _downloadUrls.Clear();
                    }

                    _downloadUrls[item.Url] = Path.GetFileName(item.Path);
                }

                downloadedItems[item.Url] = item;
            }
            else
            {
                InvokeMessageHandler($"Download failed for {item.Url}.", item, reason: ImportMessageReason.DownloadFailed);
            }
        }
    }

    protected virtual void InvokeMessageHandler(
        string msg,
        DownloadManagerItem item = null,
        ImportMessageType messageType = ImportMessageType.Info,
        ImportMessageReason reason = ImportMessageReason.None)
    {
        if (MessageHandler != null && msg.HasValue())
        {
            MessageHandler.Invoke(new ImportMessage(msg, messageType, reason)
            {
                AffectedField = item != null ? $"{item.Entity.GetEntityName()} #{item.DisplayOrder}" : null
            },
            item);
        }
    }

    private bool Succeeded(DownloadManagerItem item)
    {
        if (item.Entity == null)
        {
            InvokeMessageHandler($"{nameof(DownloadManagerItem)} does not contain the entity to which it belongs.", item, ImportMessageType.Error);
            return false;
        }

        if (!item.Success && item.ErrorMessage.HasValue())
        {
            InvokeMessageHandler(item.ToString(), item, ImportMessageType.Error);
            return false;
        }

        return item.Success;
    }

    private static string GetFileName(string fName, int maxLength = int.MaxValue, HashSet<string> lookup = null)
    {
        if (fName.IsEmpty())
        {
            return Path.GetRandomFileName();
        }

        fName = PathUtility.SanitizeFileName(fName);

        if (maxLength == int.MaxValue && lookup == null)
        {
            return fName;
        }

        var name = Path.GetFileNameWithoutExtension(fName);
        var ext = Path.GetExtension(fName);

        if (name.Length > maxLength)
        {
            name = name.Truncate(maxLength);
            fName = name + ext;
        }

        if (lookup?.Contains(fName) ?? false)
        {
            var i = 0;
            do
            {
                fName = $"{name}-{++i}{ext}";
            }
            while (lookup.Contains(fName));
        }

        return fName;
    }

    #endregion
}
