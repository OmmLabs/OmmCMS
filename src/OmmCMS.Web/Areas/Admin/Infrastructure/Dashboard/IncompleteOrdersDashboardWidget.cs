#nullable enable

using OmmCMS.Admin.Components;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Security;
using OmmCMS.Core.Widgets.Dashboard;
using OmmCMS.Engine.Modularity;

namespace OmmCMS.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders incomplete orders.
/// </summary>
[SystemName(SystemName)]
public sealed class IncompleteOrdersDashboardWidget(IPermissionService permissions)
    : DashboardViewComponentWidget<DashboardIncompleteOrdersViewComponent>(permissions, DashboardIncompleteOrdersViewComponent.RequiredPermission)
{
    /// <summary>
    /// Identifies the incomplete orders dashboard widget type.
    /// </summary>
    public const string SystemName = "OmmCMS.Admin.Dashboard.IncompleteOrders";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.SalesReport.Incomplete"))
    {
        Group = KnownDashboardWidgetGroups.Sales,
        IconName = "hourglass",
        CssClass = "report-incomplete-orders",
        Order = 100,
        DefaultSize = new DashboardWidgetSize(10, 1),
        MinSize = new DashboardWidgetSize(4, 1),
        MaxSize = new DashboardWidgetSize(12)
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;
}
