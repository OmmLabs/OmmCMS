#nullable enable

using OmmCMS.Core.Widgets.Dashboard;

namespace OmmCMS.Admin.Components;

/// <summary>
/// Resolves and renders a dashboard by its stable identifier.
/// </summary>
public sealed class DashboardViewComponent : SmartViewComponent
{
    private readonly IDashboardService _dashboardService;

    public DashboardViewComponent(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IViewComponentResult> InvokeAsync(string dashboardId, bool skipUserLayout = false, DashboardRenderModel? prepared = null)
    {
        Guard.NotEmpty(dashboardId);

        var model = prepared ?? await _dashboardService.GetDashboardAsync(
            dashboardId,
            skipUserLayout ? null : Services.WorkContext.CurrentCustomer);

        return View(model);
    }
}
