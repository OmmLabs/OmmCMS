using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.OutputCache;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Google.Analytics.Services;

namespace Smartstore.Google.Analytics;

internal class Startup : StarterBase
{
    public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
    {
        if (appContext.IsInstalled)
        {
            services.AddScoped<GoogleAnalyticsScriptHelper>();

        }
    }

    public override void BuildPipeline(RequestPipelineBuilder builder)
    {
        // OutputCache invalidation.
        var observer = builder.ApplicationBuilder.ApplicationServices.GetRequiredService<IOutputCacheInvalidationObserver>();

        observer.ObserveSettingProperty<GoogleAnalyticsSettings>(x => x.TrackingScript, x => x.RemoveAllAsync());
        observer.ObserveSettingProperty<GoogleAnalyticsSettings>(x => x.DisplayCookieInfosForAds, x => x.RemoveAllAsync());
        observer.ObserveSettingProperty<GoogleAnalyticsSettings>(x => x.GoogleId, x => x.RemoveAllAsync());
        observer.ObserveSettingProperty<GoogleAnalyticsSettings>(x => x.MinifyScripts, x => x.RemoveAllAsync());
        observer.ObserveSettingProperty<GoogleAnalyticsSettings>(x => x.RenderWithUserConsentOnly, x => x.RemoveAllAsync());
    }
}