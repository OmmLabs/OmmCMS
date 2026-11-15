#nullable enable

using OmmCMS.Admin.Components;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Security;
using OmmCMS.Core.Widgets.Dashboard;
using OmmCMS.Engine.Modularity;

namespace OmmCMS.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders customer registrations.
/// </summary>
[SystemName(SystemName)]
public sealed class RegisteredCustomersDashboardWidget(IPermissionService permissions)
    : DashboardViewComponentWidget<DashboardRegisteredCustomersViewComponent>(permissions, DashboardRegisteredCustomersViewComponent.RequiredPermission)
{
    /// <summary>
    /// Identifies the registered customers dashboard widget type.
    /// </summary>
    public const string SystemName = "OmmCMS.Admin.Dashboard.RegisteredCustomers";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.Report.CustomerRegistrations"))
    {
        Group = KnownDashboardWidgetGroups.Customers,
        IconName = "user-add",
        CssClass = "report-customer-registrations",
        Order = 500,
        DefaultSize = new DashboardWidgetSize(6, 1),
        MinSize = new DashboardWidgetSize(4, 1),
        MaxSize = new DashboardWidgetSize(12)
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;
}
