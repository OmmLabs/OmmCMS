using OmmCMS.Core.Theming;

namespace OmmCMS.Core.Data;

public partial class SmartDbContext
{
    public DbSet<ThemeVariable> ThemeVariables { get; set; }
}