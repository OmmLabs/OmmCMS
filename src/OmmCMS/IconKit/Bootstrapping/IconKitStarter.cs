using Microsoft.Extensions.DependencyInjection;
using OmmCMS.Engine;
using OmmCMS.Engine.Builders;

using OmmCMS.IconKit;
using OmmCMS.IO;

namespace OmmCMS.Bootstrapping;

/// <summary>
/// Integrates IconKit with OmmCMS's application data directory and endpoint pipeline.
/// </summary>
internal class IconKitStarter : StarterBase
{
    /// <inheritdoc />
    public override int Order => StarterOrdering.Early;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
    {
        services.AddIconKit(options =>
        {
            options.SourcePath = PathUtility.Combine(appContext.AppDataRoot.Root, "Icons");
            options.CachePath = PathUtility.Combine(appContext.AppDataRoot.Root, ".cache", "icons");
        });
    }

    /// <inheritdoc />
    public override void MapRoutes(EndpointRoutingBuilder builder)
    {
        builder.MapRoutes(StarterOrdering.EarlyRoute, routes => routes.MapIconKit());
    }
}
