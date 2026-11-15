using OmmCMS.Admin.Components;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Security;
using OmmCMS.Core.Widgets;
using OmmCMS.Core.Widgets.Dashboard;
using OmmCMS.Engine.Modularity;

namespace OmmCMS.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the widget reporting current saved cookie consent decisions.
/// </summary>
[SystemName(SystemName)]
public sealed class CookieConsentsDashboardWidget(IPermissionService permissions)
    : DashboardViewComponentWidget<DashboardCookieConsentsViewComponent>(permissions, DashboardCookieConsentsViewComponent.RequiredPermission)
{
    /// <summary>
    /// Identifies the cookie consent dashboard widget type.
    /// </summary>
    public const string SystemName = "OmmCMS.Admin.Dashboard.CookieConsents";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.Dashboard.CookieConsents"))
    {
        Description = ResolvableText.Resource("Admin.Dashboard.CookieConsents.Description"),
        Group = KnownDashboardWidgetGroups.Customers,
        IconName = "cookie",
        Order = 550,
        DefaultSize = new DashboardWidgetSize(6, 1),
        MinSize = new DashboardWidgetSize(4, 1),
        MaxSize = new DashboardWidgetSize(12)
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;
    /// <inheritdoc />
    public override Widget CreateWidget(DashboardWidgetContext context, DashboardWidgetInstance instance)
        => new ComponentWidget<DashboardCookieConsentsViewComponent>(new { refresh = context.Refresh }) { Key = instance.Id };
}
