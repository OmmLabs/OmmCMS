using Autofac;
using OmmCMS.Core.Rules;
using OmmCMS.Core.Rules.Rendering;
using OmmCMS.Engine.Builders;

namespace OmmCMS.Core.Bootstrapping;

internal class RuleStarter : StarterBase
{
    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        builder.RegisterType<RuleService>().As<IRuleService>().InstancePerLifetimeScope();
        builder.RegisterType<RuleProviderFactory>().As<IRuleProviderFactory>().InstancePerLifetimeScope();
        builder.RegisterType<RuleSetRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();

        // Rendering.
        builder.RegisterType<RuleTemplateSelector>().As<IRuleTemplateSelector>().InstancePerLifetimeScope();
    }
}