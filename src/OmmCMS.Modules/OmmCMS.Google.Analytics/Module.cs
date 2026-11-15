global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.IO;
global using System.Linq;
global using System.Threading.Tasks;
global using OmmCMS.Core.Localization;
global using OmmCMS.Google.Analytics.Settings;
global using OmmCMS.Web.Modelling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Widgets;
using OmmCMS.Engine.Modularity;
using OmmCMS.Google.Analytics.Components;
using OmmCMS.Http;

namespace OmmCMS.Google.Analytics;

internal class Module : ModuleBase, IConfigurable, IActivatableWidget, ICookiePublisher
{
    private readonly IProviderManager _providerManager;
    private readonly WidgetSettings _widgetSettings;
    private readonly GoogleAnalyticsSettings _settings;

    public Module(IProviderManager providerManager, WidgetSettings widgetSettings, GoogleAnalyticsSettings settings)
    {
        _providerManager = providerManager;
        _widgetSettings = widgetSettings;
        _settings = settings;
    }

    public ILogger Logger { get; set; } = NullLogger.Instance;
    public Localizer T { get; set; } = NullLocalizer.Instance;

    public RouteInfo GetConfigurationRoute()
        => new("Configure", "GoogleAnalytics", new { area = "Admin" });

    public Task<IEnumerable<CookieInfo>> GetCookieInfosAsync()
    {
        var widget = _providerManager.GetProvider<IActivatableWidget>("OmmCMS.Google.Analytics");
        if (!widget.IsWidgetActive(_widgetSettings))
        {
            return Task.FromResult(Enumerable.Empty<CookieInfo>());
        }

        var cookieInfos = new List<CookieInfo>
        {
            new() {
                Name = T("Plugins.FriendlyName.OmmCMS.Google.Analytics"),
                Description = T("Plugins.Widgets.GoogleAnalytics.CookieInfo"),
                CookieType = CookieType.Analytics
            }
        };

        if (_settings.DisplayCookieInfosForAds)
        {
            cookieInfos.AddRange([
                new()
                {
                    Name = T("Plugins.FriendlyName.OmmCMS.Google.Analytics"),
                    Description = T("Plugins.Widgets.GoogleAnalytics.ConsentAdUserData"),
                    CookieType = CookieType.ConsentAdUserData
                },
                new()
                {
                    Name = T("Plugins.FriendlyName.OmmCMS.Google.Analytics"),
                    Description = T("Plugins.Widgets.GoogleAnalytics.ConsentAdPersonalization"),
                    CookieType = CookieType.ConsentAdPersonalization
                }
            ]);
        }

        return Task.FromResult<IEnumerable<CookieInfo>>(cookieInfos);
    }

    public Widget GetDisplayWidget(string widgetZone, object model, int storeId)
        => new ComponentWidget(typeof(GoogleAnalyticsViewComponent), model);

    public string[] GetWidgetZones() => ["head"];

    public override async Task InstallAsync(ModuleInstallationContext context)
    {
        await ImportLanguageResourcesAsync();
        await TrySaveSettingsAsync(new GoogleAnalyticsSettings
        {
            TrackingScript = AnalyticsScriptUtility.GetTrackingScript()
        });
        await base.InstallAsync(context);
    }

    public override async Task UninstallAsync()
    {
        await DeleteLanguageResourcesAsync();
        await DeleteSettingsAsync<GoogleAnalyticsSettings>();
        await base.UninstallAsync();
    }
}