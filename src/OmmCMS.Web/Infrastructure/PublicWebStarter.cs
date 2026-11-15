using Microsoft.AspNetCore.Builder;
using OmmCMS.Engine.Builders;

namespace OmmCMS.Web.Infrastructure;

internal class PublicWebStarter : StarterBase
{
    public override void BuildPipeline(RequestPipelineBuilder builder)
    {
        if (OmmCmsMode.IsEnabled(builder.ApplicationContext.Configuration))
        {
            builder.Configure(StarterOrdering.AfterRoutingMiddleware, app =>
            {
                app.Use(OmmCmsCommerceIsolation.InvokeAsync);
            });
        }
    }
}