using Autofac;
using Microsoft.AspNetCore.Builder;
using Smartstore.Engine.Builders;

namespace Smartstore.Web.Infrastructure;

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

    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        if (appContext.IsInstalled)
        {
            builder.RegisterType<CatalogHelper>().InstancePerLifetimeScope();
            builder.RegisterType<OrderHelper>().InstancePerLifetimeScope();
        }
    }
}