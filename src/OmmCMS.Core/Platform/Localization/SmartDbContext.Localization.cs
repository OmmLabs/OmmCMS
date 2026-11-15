using OmmCMS.Core.Localization;

namespace OmmCMS.Core.Data;

public partial class SmartDbContext
{
    public DbSet<Language> Languages { get; set; }
    public DbSet<LocaleStringResource> LocaleStringResources { get; set; }
    public DbSet<LocalizedProperty> LocalizedProperties { get; set; }
}