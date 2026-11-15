using OmmCMS.Core.Configuration;

namespace OmmCMS.Core.Data;

public partial class SmartDbContext
{
    public DbSet<Setting> Settings { get; set; }
}