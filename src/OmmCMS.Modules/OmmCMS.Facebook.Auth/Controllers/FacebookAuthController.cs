using Microsoft.AspNetCore.Mvc;
using OmmCMS.ComponentModel;
using OmmCMS.Core.Security;
using OmmCMS.Engine.Modularity;
using OmmCMS.Facebook.Auth.Models;
using OmmCMS.Web.Controllers;
using OmmCMS.Web.Modelling.Settings;

namespace OmmCMS.Facebook.Auth.Controllers;

public class FacebookAuthController : AdminController
{
    private readonly IOptionsMonitorCache<FacebookOptions> _optionsCache;
    private readonly IProviderManager _providerManager;

    public FacebookAuthController(IOptionsMonitorCache<FacebookOptions> optionsCache, IProviderManager providerManager)
    {
        _optionsCache = optionsCache;
        _providerManager = providerManager;
    }

    [HttpGet, LoadSetting]
    [Permission(Permissions.Configuration.Authentication.Read)]
    public IActionResult Configure(FacebookExternalAuthSettings settings)
    {
        var model = MiniMapper.Map<FacebookExternalAuthSettings, ConfigurationModel>(settings);

        var host = Services.StoreContext.CurrentStore.GetBaseUrl();
        model.RedirectUrl = $"{host}signin-facebook";

        ViewBag.Provider = _providerManager.GetProvider("OmmCMS.Facebook.Auth").Metadata;

        return View(model);
    }

    [HttpPost, SaveSetting]
    [Permission(Permissions.Configuration.Authentication.Update)]
    public IActionResult Configure(ConfigurationModel model, FacebookExternalAuthSettings settings, int storeScope)
    {
        if (!ModelState.IsValid)
        {
            return Configure(settings);
        }

        ModelState.Clear();
        MiniMapper.Map(model, settings);

        // TODO: (mh) (core) This must also be called when settings change via all settings grid.
        _optionsCache.TryRemove(FacebookDefaults.AuthenticationScheme);

        NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

        return RedirectToAction(nameof(Configure));
    }
}