using OmmCMS.Core.Localization;

namespace OmmCMS.Core.Installation;

public class SeedDataConfiguration
{
    public string DefaultUserName { get; set; }
    public string DefaultUserPassword { get; set; }
    public Language Language { get; set; }
    /// <summary>Use CMS-only starter content and suppress commerce samples.</summary>
    public bool CmsPreview { get; set; }
    public bool SeedSampleData { get; set; } = true;
    public bool StoreMediaInDB { get; set; } = true;
    public Action<string> ProgressMessageCallback { get; set; } = (x) => { };
}