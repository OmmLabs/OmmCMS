using OmmCMS.Core.Content.Menus;

namespace OmmCMS.Core.Data;

public partial class SmartDbContext
{
    public DbSet<MenuEntity> Menus { get; set; }
    public DbSet<MenuItemEntity> MenuItems { get; set; }
}