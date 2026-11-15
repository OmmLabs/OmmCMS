using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Checkout.Payment;
using OmmCMS.Core.Data;

namespace OmmCMS.Core.Content.Media;

public partial class DownloadService : IDownloadService
{
    private readonly SmartDbContext _db;
    private readonly IMediaService _mediaService;

    public DownloadService(SmartDbContext db, IMediaService mediaService)
    {
        _db = db;
        _mediaService = mediaService;
    }

    public virtual async Task<MediaFileInfo> InsertDownloadAsync(Download download, Stream stream, string fileName)
    {
        Guard.NotNull(download);
        Guard.NotEmpty(fileName);

        var path = _mediaService.CombinePaths(SystemAlbumProvider.Downloads, fileName);
        var file = await _mediaService.SaveFileAsync(path, stream, dupeFileHandling: DuplicateFileHandling.Rename);
        file.File.Hidden = true;
        download.MediaFile = file.File;

        _db.Downloads.Add(download);
        await _db.SaveChangesAsync();

        return file;
    }

    public virtual Stream OpenDownloadStream(Download download)
    {
        Guard.NotNull(download);
        return _mediaService.StorageProvider.OpenRead(download.MediaFile);
    }

    public virtual Task<Stream> OpenDownloadStreamAsync(Download download)
    {
        Guard.NotNull(download);
        return _mediaService.StorageProvider.OpenReadAsync(download.MediaFile);
    }
}