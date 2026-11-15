using OmmCMS.Core.Common;

namespace OmmCMS.Core.Data;

public partial class SmartDbContext
{
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<GenericAttribute> GenericAttributes { get; set; }
    public DbSet<StateProvince> StateProvinces { get; set; }
}
