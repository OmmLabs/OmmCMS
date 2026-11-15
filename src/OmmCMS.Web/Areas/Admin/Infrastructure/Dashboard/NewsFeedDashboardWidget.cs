#nullable enable

using OmmCMS.Admin.Components;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Security;
using OmmCMS.Core.Widgets;
using OmmCMS.Core.Widgets.Dashboard;
using OmmCMS.Engine.Modularity;

namespace OmmCMS.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders the OmmCMS news feed.
/// </summary>
[SystemName(SystemName)]
public sealed class NewsFeedDashboardWidget(IPermissionService permissions)
    : DashboardViewComponentWidget<NewsFeedViewComponent>(permissions, NewsFeedViewComponent.RequiredPermission)
{
    /// <summary>
    /// Identifies the news feed dashboard widget type.
    /// </summary>
    public const string SystemName = "OmmCMS.Admin.Dashboard.NewsFeed";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.NewsFeed.Title"))
    {
        Group = KnownDashboardWidgetGroups.System,
        IconName = "newspaper",
        CssClass = "news-feed",
        Order = 800,
        DefaultSize = new DashboardWidgetSize(2, 1),
        MinSize = new DashboardWidgetSize(2, 1),
        MaxSize = new DashboardWidgetSize(12),
        Policy = new DashboardWidgetPolicy
        {
            IsRequired = true,
            AllowMove = false,
            AllowResize = false
        }
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;
    /// <inheritdoc />
    public override Widget CreateWidget(DashboardWidgetContext context, DashboardWidgetInstance instance)
        => new ComponentWidget<NewsFeedViewComponent>(new { refresh = context.Refresh }) { Key = instance.Id };
}
