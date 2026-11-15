using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using OmmCMS.Engine;
using OmmCMS.Engine.Builders;

namespace OmmCMS.Facebook.Auth;

internal class Startup : StarterBase
{
    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        builder.RegisterType<FacebookOptionsConfigurer>()
            .As<IConfigureOptions<AuthenticationOptions>>()
            .As<IConfigureOptions<FacebookOptions>>()
            .InstancePerDependency();

        builder.RegisterType<OAuthPostConfigureOptions<FacebookOptions, FacebookHandler>>()
            .As<IPostConfigureOptions<FacebookOptions>>()
            .InstancePerDependency();
    }
}