using OmmCMS.Core.Content.Menus;

namespace OmmCMS.Web.Models.Common;

public partial class AccountDropdownModel : EntityModelBase
{
    public string Name { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool DisplayAdminLink { get; set; }

    public List<MenuItem> MenuItems { get; } = new();
}