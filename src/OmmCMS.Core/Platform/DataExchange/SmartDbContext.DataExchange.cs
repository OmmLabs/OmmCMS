using OmmCMS.Core.DataExchange;

namespace OmmCMS.Core.Data;

public partial class SmartDbContext
{
    public DbSet<SyncMapping> SyncMappings { get; set; }
    public DbSet<ImportProfile> ImportProfiles { get; set; }
    public DbSet<ExportProfile> ExportProfiles { get; set; }
    public DbSet<ExportDeployment> ExportDeployments { get; set; }
}