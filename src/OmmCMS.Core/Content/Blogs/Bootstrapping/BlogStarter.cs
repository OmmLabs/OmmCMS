using Autofac;
using OmmCMS.Core.Content.Blogs;
using OmmCMS.Core.Seo;
using OmmCMS.Engine.Builders;

namespace OmmCMS.Core.Bootstrapping;

internal class BlogStarter : StarterBase
{
    public override bool Matches(IApplicationContext appContext) => appContext.IsInstalled;

    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        builder.RegisterType<BlogSlugService>().InstancePerLifetimeScope();
        builder.RegisterType<BlogXmlSitemapPublisher>().As<IXmlSitemapPublisher>().InstancePerLifetimeScope();
    }
}
