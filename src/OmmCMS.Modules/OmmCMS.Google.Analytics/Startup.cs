using Microsoft.Extensions.DependencyInjection;
using OmmCMS.Core.OutputCache;
using OmmCMS.Engine;
using OmmCMS.Engine.Builders;
using OmmCMS.Google.Analytics.Services;

namespace OmmCMS.Google.Analytics;

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