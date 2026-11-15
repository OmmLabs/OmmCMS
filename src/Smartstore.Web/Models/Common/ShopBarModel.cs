namespace Smartstore.Web.Models.Common;

public partial class ShopBarModel : ModelBase
{
    public bool IsAuthenticated { get; set; }
    public string CustomerEmailUsername { get; set; }
    public bool IsCustomerImpersonated { get; set; }
    public bool DisplayAdminLink { get; set; }
    public bool PublicStoreNavigationAllowed { get; set; }
}