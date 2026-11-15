using OmmCMS.Core.Stores;

namespace OmmCMS.Core.Data;

public partial class SmartDbContext
{
    public DbSet<Store> Stores { get; set; }
    public DbSet<StoreMapping> StoreMappings { get; set; }
}