using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using OmmCMS.Engine;
using OmmCMS.Engine.Builders;

namespace OmmCMS.Apple.Auth;

internal class Startup : StarterBase
{
    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        builder.RegisterType<AppleOptionsConfigurer>()
            .As<IConfigureOptions<AuthenticationOptions>>()
            .As<IConfigureOptions<AppleAuthenticationOptions>>()
            .InstancePerDependency();

        builder.RegisterType<OAuthPostConfigureOptions<AppleAuthenticationOptions, AppleAuthenticationHandler>>()
            .As<IPostConfigureOptions<AppleAuthenticationOptions>>()
            .InstancePerDependency();
    }
}