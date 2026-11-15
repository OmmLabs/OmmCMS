using Autofac;
using OmmCMS.Core.Checkout.Rules;
using OmmCMS.Core.Checkout.Shipping;
using OmmCMS.Core.Identity.Rules;
using OmmCMS.Core.Rules;
using OmmCMS.Core.Rules.Rendering;
using OmmCMS.Engine.Builders;
using OmmCMS.Net.Http;

namespace OmmCMS.Core.Bootstrapping;

internal sealed class CheckoutStarter : StarterBase
{
    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {

        // Cart rules.
        var cartRuleTypes = appContext.TypeScanner.FindTypes<IRule<CartRuleContext>>().ToList();
        foreach (var ruleType in cartRuleTypes)
        {
            builder.RegisterType(ruleType).Keyed<IRule<CartRuleContext>>(ruleType).InstancePerLifetimeScope();
        }

        builder.RegisterType<CartRuleProvider>().As<ICartRuleProvider>().InstancePerLifetimeScope();

        // Rule options provider.
        builder.RegisterType<AuthenticationMethodRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
    }

}
