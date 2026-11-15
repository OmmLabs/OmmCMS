using Smartstore.Admin.Models.Common;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Components;

public class NewsFeedViewComponent : SmartViewComponent
{
    public const string RequiredPermission = Permissions.Dashboard.Widgets.System.NewsFeed;

    public async Task<IViewComponentResult> InvokeAsync(bool refresh = false)
    {
        if (!await Services.Permissions.AuthorizeAsync(RequiredPermission))
        {
            return Empty();
        }

        return View(new FeedModel
        {
            NewsFeedCannels = new()
            {
                new NewsFeedChannelModel
                {
                    Name = "OmmCMS",
                    ButtonText = "OmmLabs website",
                    ButtonLink = "https://ommlabs.ommrudraksha.com",
                    NewsFeedItems = new()
                    {
                        new NewsFeedItemModel
                        {
                            Title = "OmmCMS project updates",
                            Short = "Follow development and release information on GitHub.",
                            Link = "https://github.com/OmmLabs/OmmCMS",
                            ViewType = "full"
                        }
                    }
                }
            }
        });
    }
}
