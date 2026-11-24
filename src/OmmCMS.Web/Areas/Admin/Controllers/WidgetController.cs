using OmmCMS.Admin.Models.Modularity;
using OmmCMS.Core.Security;
using OmmCMS.Engine.Modularity;

namespace OmmCMS.Admin.Controllers;

public partial class WidgetController : AdminController
{
    private readonly IWidgetService _widgetService;
    private readonly IProviderManager _providerManager;
    private readonly WidgetSettings _widgetSettings;
    private readonly ModuleManager _moduleManager;

    public WidgetController(
        IWidgetService widgetService,
        IProviderManager providerManager,
        WidgetSettings widgetSettings,
        ModuleManager moduleManager)
    {
        _widgetService = widgetService;
        _providerManager = providerManager;
        _widgetSettings = widgetSettings;
        _moduleManager = moduleManager;
    }

    public IActionResult Index()
    {
        return RedirectToAction("Providers");
    }

    [Permission(Permissions.Cms.Widget.Read)]
    public async Task<IActionResult> Providers()
    {
        var canUpdate = await Services.Permissions.AuthorizeAsync(Permissions.Cms.Widget.Update);
        ViewBag.CanActivateProvider = await Services.Permissions.AuthorizeAsync(Permissions.Cms.Widget.Activate);
        var widgetsModel = new List<WidgetModel>();
        var widgets = _providerManager.GetAllProviders<IActivatableWidget>();

        foreach (var widget in widgets)
        {
            var model = _moduleManager.ToProviderModel<IActivatableWidget, WidgetModel>(widget);
            model.IsEditable = model.IsEditable && canUpdate;
            model.IsActive = widget.IsWidgetActive(_widgetSettings);
            widgetsModel.Add(model);
        }

        return View(widgetsModel);
    }

    [HttpPost]
    [Permission(Permissions.Cms.Widget.Activate)]
    public async Task<IActionResult> ActivateProvider(string systemName, bool activate)
    {
        await _widgetService.ActivateWidgetAsync(systemName, activate);
        return RedirectToAction("Providers");
    }
}