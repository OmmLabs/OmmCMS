using OmmCMS.Core.Configuration;
using OmmCMS.Core.Content.Media;

namespace OmmCMS.Core.Common.Configuration;

public class PerformanceSettings : ISettings
{
    /// <summary>
    /// The number of entries in a single cache segment
    /// when greedy loading is disabled. The larger the website,
    /// the smaller this value should be. We recommend segment
    /// size 500 for websites with fewer than 100,000 records.
    /// </summary>
    public int CacheSegmentSize { get; set; } = 500;

    /// <summary>
    /// By default translations are loaded through the segmented cache.
    /// For very large multilingual websites, setting this to true
    /// can result in higher request performance as well as less
    /// resource usage.
    /// </summary>
    public bool AlwaysPrefetchTranslations { get; set; }

    /// <summary>
    /// By default URL slugs are loaded through the segmented cache.
    /// For very large websites, setting this to true
    /// can result in higher request performance as well as less
    /// resource usage.
    /// </summary>
    public bool AlwaysPrefetchUrlSlugs { get; set; }

    /// <summary>
    /// Enables response compression for text-based static and dynamic responses
    /// (html, css, js, svg etc.). Turn this off if the webserver handles response
    /// compression already. Changing the value requires an application restart to take effect.
    /// </summary>
    public bool UseResponseCompression { get; set; }

    /// <summary>
    /// Maximum number of MediaFile entities to cache for detecting duplicate files.
    /// If a media folder contains more files, no caching is done for scalability reasons
    /// and the <see cref="MediaFile"/> entities are loaded directly from the database.
    /// </summary>
    public int MediaDupeDetectorMaxCacheSize { get; set; } = 10000;

    /// <summary>
    /// The maximum number of guest customers to delete in a single batch.
    /// This hidden setting helps manage performance and timeout issues when cleaning up guest accounts.
    /// Default value is 5000.
    /// </summary>
    public int GuestCustomersDeletionBatchSize { get; set; } = 5000;

    /// <summary>
    /// Command timeout in seconds used exclusively while deleting guest customers.
    /// This hidden setting helps manage performance and timeout issues when cleaning up guest accounts.
    /// Default value is 0 to use the database default command timeout.
    /// </summary>
    public int GuestCustomersDeletionCommandTimeout { get; set; }
}
