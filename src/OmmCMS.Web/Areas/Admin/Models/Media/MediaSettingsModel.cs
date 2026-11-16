using System.ComponentModel.DataAnnotations;
using FluentValidation;
using OmmCMS.ComponentModel;
using OmmCMS.Core.Content.Media;
using OmmCMS.Imaging;

namespace OmmCMS.Admin.Models.Media;

[LocalizedDisplay("Admin.Configuration.Settings.Media.")]
public class MediaSettingsModel : ModelBase
{
    [LocalizedDisplay("*AutoGenerateAbsoluteUrls")]
    public bool AutoGenerateAbsoluteUrls { get; set; }

    [LocalizedDisplay("*MaximumImageSize")]
    public int MaximumImageSize { get; set; }

    [LocalizedDisplay("*MaxUploadFileSize")]
    public long MaxUploadFileSize { get; set; } = 102400;

    [LocalizedDisplay("*MakeFilesTransientWhenOrphaned")]
    public bool MakeFilesTransientWhenOrphaned { get; set; }

    [LocalizedDisplay("*DefaultPictureZoomEnabled")]
    public bool DefaultPictureZoomEnabled { get; set; }

    [LocalizedDisplay("*StorageProvider")]
    public string StorageProvider { get; set; }

    #region Thumbnail sizes

    [LocalizedDisplay("*AvatarPictureSize")]
    public int AvatarPictureSize { get; set; }

    [LocalizedDisplay("*PreviewThumbnailSize")]
    public int PreviewThumbnailSize { get; set; }

    public int[] CurrentlyAllowedThumbnailSizes { get; set; }

    #endregion

    #region Media types

    [UIHint("Textarea")]
    [AdditionalMetadata("rows", 2)]
    [LocalizedDisplay("*Type.Image")]
    public string ImageTypes { get; set; }

    [UIHint("Textarea")]
    [AdditionalMetadata("rows", 2)]
    [LocalizedDisplay("*Type.Video")]
    public string VideoTypes { get; set; }

    [UIHint("Textarea")]
    [AdditionalMetadata("rows", 2)]
    [LocalizedDisplay("*Type.Audio")]
    public string AudioTypes { get; set; }

    [UIHint("Textarea")]
    [AdditionalMetadata("rows", 2)]
    [LocalizedDisplay("*Type.Document")]
    public string DocumentTypes { get; set; }

    [UIHint("Textarea")]
    [AdditionalMetadata("rows", 2)]
    [LocalizedDisplay("*Type.Text")]
    public string TextTypes { get; set; }

    [UIHint("Textarea")]
    [AdditionalMetadata("rows", 2)]
    [LocalizedDisplay("*Type.Bin")]
    public string BinTypes { get; set; }

    #endregion

    #region Image processing

    public ResamplingMode DefaultResamplingMode { get; set; }

    [AdditionalMetadata("min", 0)]
    [AdditionalMetadata("max", 100)]
    public int DefaultImageQuality { get; set; }

    [LocalizedDisplay("*JpegColorType")]
    public JpegColorType? JpegColorType { get; set; }

    public PngCompressionLevel PngCompressionLevel { get; set; }

    [LocalizedDisplay("*PngQuantizationMethod")]
    public QuantizationMethod PngQuantizationMethod { get; set; }

    [LocalizedDisplay("*PngInterlaced")]
    public bool PngInterlaced { get; set; }

    public bool PngIgnoreMetadata { get; set; }

    [LocalizedDisplay("*GifQuantizationMethod")]
    public QuantizationMethod GifQuantizationMethod { get; set; }

    [LocalizedDisplay("*OffloadEmbeddedImagesOnSave")]
    public bool OffloadEmbeddedImagesOnSave { get; set; }

    #endregion

    #region Response Caching

    [AdditionalMetadata("min", 0)]
    public int ResponseCacheDuration { get; set; }

    public ResponseCacheLocation ResponseCacheLocation { get; set; }

    public bool ResponseCacheNoStore { get; set; }

    public bool AppendFileVersionToUrl { get; set; }

    #endregion
}

public partial class MediaSettingsValidator : SettingModelValidator<MediaSettingsModel, MediaSettings>
{
    public MediaSettingsValidator()
    {
        // INFO: Such a rule for a settings model requires that the model property has a valid default value.
        // Typically, this is the same as in the settings class.
        // Otherwise you will run into a validation error if you deactivate the multistore checkbox.
        RuleFor(x => x.MaxUploadFileSize).GreaterThan(0);
        RuleFor(x => x.MaximumImageSize).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AvatarPictureSize).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PreviewThumbnailSize).GreaterThanOrEqualTo(0);
    }
}

[Mapper(Lifetime = ServiceLifetime.Singleton)]
public class MediaSettingsMapper :
    IMapper<MediaSettings, MediaSettingsModel>,
    IMapper<MediaSettingsModel, MediaSettings>
{
    public Task MapAsync(MediaSettings from, MediaSettingsModel to, dynamic parameters = null)
    {
        MiniMapper.Map(from, to);

        to.ImageTypes = MapMediaType(from.ImageTypes, MediaType.Image);
        to.VideoTypes = MapMediaType(from.VideoTypes, MediaType.Video);
        to.AudioTypes = MapMediaType(from.AudioTypes, MediaType.Audio);
        to.DocumentTypes = MapMediaType(from.DocumentTypes, MediaType.Document);
        to.TextTypes = MapMediaType(from.TextTypes, MediaType.Text);
        to.BinTypes = MapMediaType(from.BinTypes, MediaType.Binary);

        return Task.CompletedTask;
    }

    public Task MapAsync(MediaSettingsModel from, MediaSettings to, dynamic parameters = null)
    {
        MiniMapper.Map(from, to);

        to.ImageTypes = MapMediaType(from.ImageTypes, MediaType.Image);
        to.VideoTypes = MapMediaType(from.VideoTypes, MediaType.Video);
        to.AudioTypes = MapMediaType(from.AudioTypes, MediaType.Audio);
        to.DocumentTypes = MapMediaType(from.DocumentTypes, MediaType.Document);
        to.TextTypes = MapMediaType(from.TextTypes, MediaType.Text);
        to.BinTypes = MapMediaType(from.BinTypes, MediaType.Binary);

        return Task.CompletedTask;
    }

    private static string MapMediaType(string types, MediaType mediaType)
    {
        return types.NullEmpty() ?? string.Join(' ', mediaType.DefaultExtensions);
    }
}
