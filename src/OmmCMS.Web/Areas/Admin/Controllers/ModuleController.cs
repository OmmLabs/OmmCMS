using System.Linq;
using Microsoft.AspNetCore.Routing;
using OmmCMS.Admin.Models.Modularity;
using OmmCMS.Admin.Models.Stores;
using OmmCMS.ComponentModel;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Security;
using OmmCMS.Core.Stores;
using OmmCMS.Engine.Modularity;

namespace OmmCMS.Admin.Controllers;

public class ModuleController : AdminController
{
    private readonly IModuleCatalog _moduleCatalog;
    private readonly ILanguageService _languageService;
    private readonly IXmlResourceManager _xmlResourceManager;
    private readonly IProviderManager _providerManager;
    private readonly ModuleManager _moduleManager;

    public ModuleController(
        IModuleCatalog moduleCatalog,
        ILanguageService languageService,
        IXmlResourceManager xmlResourceManager,
        IProviderManager providerManager,
        ModuleManager moduleManager)
    {
        _moduleCatalog = moduleCatalog;
        _languageService = languageService;
        _xmlResourceManager = xmlResourceManager;
        _providerManager = providerManager;
        _moduleManager = moduleManager;
    }

    #region Modules

    public IActionResult Index()
    {
        return RedirectToAction(nameof(List));
    }

    [Permission(Permissions.Configuration.Module.Read)]
    public async Task<IActionResult> List()
    {
        var model = await PrepareLocalModulesModel();
        return View(model);
    }

    [HttpPost]
    [Permission(Permissions.Configuration.Module.Install)]
    public async Task<IActionResult> ExecuteTasks(IEnumerable<string> modulesToInstall, IEnumerable<string> modulesToUninstall)
    {
        var installNames = (modulesToInstall ?? Enumerable.Empty<string>()).ToArray();
        var uninstallNames = (modulesToUninstall ?? Enumerable.Empty<string>()).ToArray();

        if (!ModelState.IsValid || installNames.Concat(uninstallNames).Any(string.IsNullOrWhiteSpace)
            || installNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != installNames.Length
            || uninstallNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != uninstallNames.Length
            || installNames.Intersect(uninstallNames, StringComparer.OrdinalIgnoreCase).Any())
        {
            return BadRequest();
        }

        var install = installNames.Select(name => _moduleCatalog.GetModuleByName(name, false)).ToArray();
        var uninstall = uninstallNames.Select(name => _moduleCatalog.GetModuleByName(name)).ToArray();

        if (install.Any(x => x == null || x.Incompatible || x.IsInstalled()) || uninstall.Any(x => x == null))
        {
            return BadRequest();
        }

        try
        {
            var numTasks = 0;

            // Uninstall first, after validating the entire request.
            foreach (var descriptor in uninstall)
            {
                var instance = _moduleManager.CreateInstance(descriptor);
                await instance.UninstallAsync();
                numTasks++;
            }

            // Execute installations on next app start using canonical module names.
            if (install.Length > 0)
            {
                ModularState.Instance.PendingModules.AddRange(install.Select(x => x.SystemName));
                ModularState.Instance.Save();
                numTasks += install.Length;
            }

            // Restart application.
            if (numTasks > 0)
            {
                return RedirectToAction("RestartApplication", "Maintenance", new { returnUrl = Url.Action("List") });
            }
        }
        catch (Exception ex)
        {
            NotifyError(ex);
        }

        return RedirectToAction(nameof(List));
    }

    [Permission(Permissions.System.Maintenance.Execute)]
    public IActionResult ReloadList()
    {
        return RedirectToAction("RestartApplication", "Maintenance", new { returnUrl = Url.Action("List") });
    }

    [Permission(Permissions.Configuration.Module.Read)]
    public async Task<IActionResult> ConfigureModule(string systemName)
    {
        var descriptor = _moduleCatalog.GetModuleByName(systemName);
        if (descriptor == null || !descriptor.Module.IsConfigurable)
        {
            return NotFound();
        }

        var model = await PrepareModuleModelAsync(descriptor, false);
        var route = model.ConfigurationRoute;

        if (route == null)
        {
            return NotFound();
        }

        var routeValues = route.RouteValues;
        if (!routeValues.ContainsKey("returnUrl"))
        {
            routeValues = new RouteValueDictionary(routeValues);
            routeValues.Merge("returnUrl", Url.Referrer().EmptyNull(), false);
        }

        return RedirectToAction(route.Action, route.Controller, routeValues);
    }

    [HttpPost]
    [Permission(Permissions.Configuration.Module.Update)]
    public async Task<IActionResult> SetSelectedStores(string systemName, int[] storeIds)
    {
        try
        {
            var descriptor = _moduleCatalog.GetModuleByName(systemName, false);
            if (descriptor == null)
            {
                return NotFound("The plugin does not exist.");
            }

            if (!ModelState.IsValid)
                return BadRequest();

            storeIds = (storeIds ?? Array.Empty<int>()).Distinct().ToArray();
            var availableStoreIds = Services.StoreContext.GetAllStores().Select(x => x.Id).ToHashSet();
            if (storeIds.Any(id => !availableStoreIds.Contains(id)))
                return BadRequest();

            var settingKey = descriptor.GetSettingKey("LimitedToStores");

            if (storeIds.Length > 0)
            {
                await Services.Settings.ApplySettingAsync(settingKey, string.Join(',', storeIds));
            }
            else
            {
                await Services.Settings.RemoveSettingAsync(settingKey);
            }

            await Services.DbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(501, ex.Message);
        }

        return StatusCode(200);
    }

    [HttpPost]
    [Permission(Permissions.Configuration.Module.Update)]
    public async Task<IActionResult> UpdateStringResources(string systemName)
    {
        var moduleDescriptor = _moduleCatalog.GetModuleByName(systemName);

        var success = false;
        var message = string.Empty;
        if (moduleDescriptor == null)
        {
            message = T("Admin.Configuration.Plugins.Resources.UpdateFailure").Value;
        }
        else
        {
            await _xmlResourceManager.ImportModuleResourcesFromXmlAsync(moduleDescriptor, null, false);

            success = true;
            message = T("Admin.Configuration.Plugins.Resources.UpdateSuccess").Value;
        }

        return Json(new
        {
            Success = success,
            Message = message
        });
    }

    [HttpPost]
    [Permission(Permissions.Configuration.Module.Update)]
    public async Task<IActionResult> UpdateAllStringResources()
    {
        var moduleDescriptors = _moduleCatalog.Modules;

        foreach (var descriptor in moduleDescriptors)
        {
            if (descriptor.IsInstalled())
            {
                await _xmlResourceManager.ImportModuleResourcesFromXmlAsync(descriptor, null, false);
            }
            else
            {
                await Services.Localization.DeleteLocaleStringResourcesAsync(descriptor.ResourceRootKey);
            }
        }

        return Json(new
        {
            Success = true,
            Message = T("Admin.Configuration.Plugins.Resources.UpdateSuccess").Value
        });
    }

    protected async Task<ModuleModel> PrepareModuleModelAsync(IModuleDescriptor descriptor, bool forList = true)
    {
        var model = await MapperFactory.MapAsync<IModuleDescriptor, ModuleModel>(descriptor, parameters: null);

        // Using GetResource because T could fallback to NullLocalizer here.
        model.Group = Services.Localization.GetResource("Admin.Plugins.KnownGroup." + descriptor.Group);

        model.Installed = descriptor.IsInstalled();
        model.IsConfigurable = descriptor.Module?.IsConfigurable == true;

        if (forList)
        {
            model.FriendlyName = _moduleManager.GetLocalizedValue(descriptor, x => x.FriendlyName);
            model.Description = _moduleManager.GetLocalizedValue(descriptor, x => x.Description);
        }

        // Locales
        AddLocales(model.Locales, (locale, languageId) =>
        {
            locale.FriendlyName = _moduleManager.GetLocalizedValue(descriptor, x => x.FriendlyName, languageId, false);
            locale.Description = _moduleManager.GetLocalizedValue(descriptor, x => x.Description, languageId, false);
        });

        // Stores
        model.SelectedStoreIds = (await Services.Settings.GetSettingByKeyAsync<string>(descriptor.GetSettingKey("LimitedToStores"))).ToIntArray();

        // Icon
        model.Icon = _moduleManager.GetIcon(descriptor);

        if (model.Installed)
        {
            if (descriptor.Module.IsConfigurable)
            {
                // Specify configuration URL only when a module is already installed
                model.ConfigurationUrl = Url.Action("ConfigureModule", new { systemName = descriptor.SystemName });

                if (!forList)
                {
                    var configurable = _moduleManager.CreateInstance(descriptor) as IConfigurable;
                    model.ConfigurationRoute = configurable.GetConfigurationRoute();
                }
            }

        }

        return model;
    }

    protected async Task<LocalModulesModel> PrepareLocalModulesModel()
    {
        var modules = await _moduleCatalog.Modules
            .OrderBy(p => p.Group, ModuleDescriptor.KnownGroupComparer)
            .ThenByDescending(p => p.IsInstalled())
            .ThenBy(p => p.Order)
            .SelectAwait(x => PrepareModuleModelAsync(x))
            .ToListAsync();

        var model = new LocalModulesModel
        {
            AvailableStores = await Services.StoreContext
                .GetAllStores()
                .SelectAwait(s => MapperFactory.MapAsync<Store, StoreModel>(s, parameters: null))
                .ToListAsync()
        };

        var groupedModules = from p in modules
                             group p by p.Group into g
                             select g;

        foreach (var group in groupedModules)
        {
            foreach (var module in group)
            {
                model.Groups.Add(group.Key, module);
            }
        }

        return model;
    }

    #endregion

    #region Providers

    [Permission(Permissions.Configuration.Module.Read)]
    public async Task<IActionResult> ConfigureProvider(string systemName)
    {
        var provider = _providerManager.GetProvider(systemName);
        if (provider == null || !provider.Metadata.IsConfigurable)
        {
            return NotFound();
        }

        var infos = GetProviderInfos(provider);
        if (!await Services.Permissions.AuthorizeAsync(infos.ReadPermission ?? Permissions.Configuration.Module.Read))
        {
            throw new AccessDeniedException();
        }

        var model = _moduleManager.ToProviderModel(provider);
        var route = model.ConfigurationRoute;

        if (route == null)
        {
            return NotFound();
        }

        var routeValues = route.RouteValues;
        if (!routeValues.ContainsKey("returnUrl"))
        {
            routeValues = new RouteValueDictionary(routeValues);
            routeValues.Merge("returnUrl", Url.Referrer().EmptyNull(), false);
        }

        return RedirectToAction(route.Action, route.Controller, routeValues);
    }

    public async Task<IActionResult> EditProviderPopup(string systemName)
    {
        var provider = _providerManager.GetProvider(systemName);
        if (provider == null)
        {
            return NotFound();
        }

        var infos = GetProviderInfos(provider);

        if (!await Services.Permissions.AuthorizeAsync(infos.ReadPermission ?? Permissions.Configuration.Module.Read))
        {
            throw new AccessDeniedException();
        }

        var model = _moduleManager.ToProviderModel(provider, true);
        var pageTitle = model.FriendlyName;

        AddLocales(model.Locales, (locale, languageId) =>
        {
            locale.FriendlyName = _moduleManager.GetLocalizedFriendlyName(provider.Metadata, languageId, false);
            locale.Description = _moduleManager.GetLocalizedDescription(provider.Metadata, languageId, false);

            if (pageTitle.IsEmpty() && languageId == Services.WorkContext.WorkingLanguage.Id)
            {
                pageTitle = locale.FriendlyName;
            }
        });

        ViewBag.Title = pageTitle;
        ViewBag.CanUpdateProvider = await Services.Permissions.AuthorizeAsync(infos.UpdatePermission ?? Permissions.Configuration.Module.Update);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditProviderPopup(string btnId, ProviderModel model)
    {
        var provider = _providerManager.GetProvider(model.SystemName);
        if (provider == null)
        {
            return NotFound();
        }

        var infos = GetProviderInfos(provider);

        if (!await Services.Permissions.AuthorizeAsync(infos.UpdatePermission ?? Permissions.Configuration.Module.Update))
        {
            throw new AccessDeniedException();
        }

        await _moduleManager.ApplySettingAsync(provider.Metadata, nameof(model.FriendlyName), model.FriendlyName);
        await _moduleManager.ApplySettingAsync(provider.Metadata, nameof(model.Description), model.Description);

        foreach (var localized in model.Locales)
        {
            await _moduleManager.ApplyLocalizedValueAsync(provider.Metadata, localized.LanguageId, nameof(model.FriendlyName), localized.FriendlyName);
            await _moduleManager.ApplyLocalizedValueAsync(provider.Metadata, localized.LanguageId, nameof(model.Description), localized.Description);
        }

        ViewBag.RefreshPage = true;
        ViewBag.btnId = btnId;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SortProviders(string providers)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(providers))
                return BadRequest();

            var arr = providers.Split(',', StringSplitOptions.TrimEntries);
            if (arr.Any(string.IsNullOrWhiteSpace) || arr.Distinct(StringComparer.OrdinalIgnoreCase).Count() != arr.Length)
                return BadRequest();

            var selected = arr.Select(x => _providerManager.GetProvider(x)).ToArray();
            if (selected.Any(x => x == null))
                return NotFound();

            foreach (var provider in selected)
            {
                var infos = GetProviderInfos(provider);
                if (!await Services.Permissions.AuthorizeAsync(infos.UpdatePermission ?? Permissions.Configuration.Module.Update))
                    return Forbid();
            }

            var ordinal = 5;
            foreach (var provider in selected)
            {
                await _moduleManager.SetUserDisplayOrderAsync(provider.Metadata, ordinal);
                ordinal += 5;
            }
        }
        catch (Exception ex)
        {
            NotifyError(ex.Message);
            return StatusCode(501, ex.Message);
        }

        return StatusCode(200);
    }

    private static (string ReadPermission, string UpdatePermission) GetProviderInfos(Provider<IProvider> provider)
    {
        string readPermission = null;
        string updatePermission = null;

        var metadata = provider.Metadata;

        if (metadata.ProviderType == typeof(IActivatableWidget))
        {
            readPermission = Permissions.Cms.Widget.Read;
            updatePermission = Permissions.Cms.Widget.Update;
        }
        else if (metadata.ProviderType == typeof(IExternalAuthenticationMethod))
        {
            readPermission = Permissions.Configuration.Authentication.Read;
            updatePermission = Permissions.Configuration.Authentication.Update;
        }

        return (readPermission, updatePermission);
    }

    #endregion
}
