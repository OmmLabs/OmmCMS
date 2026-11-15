using OmmCMS.Collections;
using OmmCMS.Core.Content.Topics;
using OmmCMS.Core.Data;
using OmmCMS.Data.Hooks;

namespace OmmCMS.Core.Content.Media.Hooks;

internal class ImageOffloaderHook : AsyncDbSaveHook<BaseEntity>
{
    private readonly Lazy<IImageOffloder> _imageOffloader;
    private readonly Lazy<SmartDbContext> _db;
    private readonly MediaSettings _mediaSettings;
    private readonly HashSet<BaseEntity> _toProcess = new();

    public ImageOffloaderHook(
        Lazy<IImageOffloder> imageOffloader,
        Lazy<SmartDbContext> db,
        MediaSettings mediaSettings)
    {
        _imageOffloader = imageOffloader;
        _db = db;
        _mediaSettings = mediaSettings;
    }

    private static bool IsValidEntry(IHookedEntity entry)
    {
        if (entry.InitialState == EntityState.Deleted)
        {
            return false;
        }

        var t = entry.EntityType;
        return t == typeof(Topic);
    }

    public override Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
    {
        if (!IsValidEntry(entry))
        {
            return Task.FromResult(HookResult.Void);
        }

        if (!_mediaSettings.OffloadEmbeddedImagesOnSave)
        {
            return Task.FromResult(HookResult.Ok);
        }

        if (entry.InitialState == EntityState.Added)
        {
            _toProcess.Add(entry.Entity);
        }
        else if (entry.InitialState == EntityState.Modified)
        {
            var isModified = entry.IsPropertyModified(nameof(Topic.Body));

            if (isModified)
            {
                _toProcess.Add(entry.Entity);
            }
        }

        return Task.FromResult(HookResult.Ok);
    }

    public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        => Task.FromResult(IsValidEntry(entry)
                ? HookResult.Ok
                : HookResult.Void);

    public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
    {
        if (_toProcess.Count == 0)
        {
            return;
        }

        TreeNode<MediaFolderNode> folder = null;

        var offloader = _imageOffloader.Value;
        var numSucceeded = 0;

        foreach (var entry in entries)
        {
            if (_toProcess.Contains(entry.Entity))
            {
                // Get the property name of the entities long HTML description.
                var propName = nameof(Topic.Body);

                // Get the HTML
                var entityProperty = entry.Entry.Property(propName);
                var html = (string)entityProperty.CurrentValue;

                // Short HTML most likely does not contain embedded images.
                if (html.HasValue() && html.Length > 50 && offloader.HasEmbeddedImage(html))
                {
                    // Get or create destination folder
                    folder ??= await offloader.GetDefaultMediaFolderAsync();

                    // Get the file name prefix (tag)
                    var entityTag = GetEntityTag(entry.Entity);

                    try
                    {
                        // Try extraction now
                        var offloadResult = await offloader.OffloadEmbeddedImagesAsync(html, folder.Value, entityTag);
                        numSucceeded += offloadResult.NumSucceded;
                        if (offloadResult.NumSucceded > 0)
                        {
                            // At least one image was extracted: set the processed HTML as entity current value.
                            entityProperty.CurrentValue = offloadResult.ResultHtml;
                        }
                    }
                    catch
                    {
                        // Something went wrong. Just ignore and do nothing.
                    }
                }
            }
        }

        _toProcess.Clear();

        if (numSucceeded > 0)
        {
            await _db.Value.SaveChangesAsync(cancelToken);
        }
    }

    private static string GetEntityTag(BaseEntity entity) => "t" + entity.Id.ToStringInvariant();
}
