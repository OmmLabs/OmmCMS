using OmmCMS.Core.Seo;

namespace OmmCMS.Core.Data;

public partial class SmartDbContext
{
    public DbSet<UrlRecord> UrlRecords { get; set; }
}