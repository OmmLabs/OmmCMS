using Autofac;
using OmmCMS.Core.Content.Topics;
using OmmCMS.Core.Seo;
using OmmCMS.Core.Widgets;
using OmmCMS.Engine.Builders;

namespace OmmCMS.Core.Bootstrapping;

internal class TopicStarter : StarterBase
{
    public override bool Matches(IApplicationContext appContext)
        => appContext.IsInstalled;

    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        builder.RegisterType<TopicXmlSitemapPublisher>().As<IXmlSitemapPublisher>().InstancePerLifetimeScope();
        builder.RegisterType<TopicWidgetSource>().As<IWidgetSource>().InstancePerLifetimeScope();
    }
}