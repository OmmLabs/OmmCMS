using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Theming;

namespace Smartstore.Web.Controllers;

[CookieConsent]
[PreviewMode]
[CheckStoreClosed]
[AuthorizeShopAccess]
[TrackActivity(Order = 100)]
[SaveChanges<SmartDbContext>(Order = int.MaxValue)]
public class PublicController : SmartController
{
}
