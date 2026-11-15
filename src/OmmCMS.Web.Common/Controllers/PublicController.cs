using OmmCMS.Core.Identity;
using OmmCMS.Core.Security;
using OmmCMS.Core.Stores;
using OmmCMS.Web.Theming;

namespace OmmCMS.Web.Controllers;

[CookieConsent]
[PreviewMode]
[CheckStoreClosed]
[AuthorizeShopAccess]
[TrackActivity(Order = 100)]
[SaveChanges<SmartDbContext>(Order = int.MaxValue)]
public class PublicController : SmartController
{
}
