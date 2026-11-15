using System.Net;
using System.Net.Http;
using Autofac;
using OmmCMS.Core.AI;
using OmmCMS.Core.AI.Metadata;
using OmmCMS.Core.AI.Prompting;
using OmmCMS.Engine.Builders;
using OmmCMS.Net.Http;

namespace OmmCMS.Core.Bootstrapping;

internal sealed class AIStarter : StarterBase
{
    public override bool Matches(IApplicationContext appContext)
        => appContext.IsInstalled;

    public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
    {
        services.AddHttpClient("AIRemoteMetadata")
            .AddOmmCMSUserAgent()
            .ConfigurePrimaryHttpMessageHandler(c => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip
            })
            .ConfigureHttpClient(client =>
            {
                if (Uri.TryCreate(appContext.AppConfiguration.AIRemoteMetadataUrl, UriKind.Absolute, out var metadataUrl))
                {
                    client.BaseAddress = metadataUrl;
                }
                client.Timeout = TimeSpan.FromSeconds(2);
            });
    }

    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        // Register all prompt generators.
        var promptGeneratorTypes = appContext.TypeScanner.FindTypes<IAIPromptGenerator>();
        foreach (var type in promptGeneratorTypes)
        {
            builder.RegisterType(type).As<IAIPromptGenerator>().Keyed<IAIPromptGenerator>(type).InstancePerLifetimeScope();
        }

        builder.RegisterType<RemoteAIMetadataLoader>().As<IRemoteAIMetadataLoader>().SingleInstance();
        builder.RegisterType<DefaultAIMetadataLoader>().As<IAIMetadataLoader>().SingleInstance();
        builder.RegisterType<DefaultAIChatCache>().As<IAIChatCache>().SingleInstance();
        builder.RegisterType<AIMessageBuilder>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<AIMessageResources>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<AIProviderFactory>().As<IAIProviderFactory>().InstancePerLifetimeScope();
        builder.RegisterType<DefaultLlmsGenerator>().As<ILlmsGenerator>().InstancePerLifetimeScope();
    }
}
