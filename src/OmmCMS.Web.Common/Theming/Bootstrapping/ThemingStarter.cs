using Autofac;
using OmmCMS.Core.Bootstrapping;
using OmmCMS.Core.Theming;
using OmmCMS.Engine.Builders;
using OmmCMS.Web.Theming;

namespace OmmCMS.Web.Bootstrapping;

internal sealed class ThemingStarter : StarterBase
{
    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        builder.RegisterModule(new ThemesModule());
        builder.RegisterType<ThemeVariableRepository>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<DefaultThemeVariableService>().As<IThemeVariableService>().InstancePerLifetimeScope();
        builder.RegisterType<DefaultThemeContext>().As<IThemeContext>().InstancePerLifetimeScope();
    }

    public override void BuildPipeline(RequestPipelineBuilder builder)
    {
        var themeRegistry = builder.ApplicationBuilder.ApplicationServices.GetService<IThemeRegistry>();
        if (themeRegistry != null)
        {
            themeRegistry.ThemeExpired += OnThemeExpired;
        }
    }

    private static void OnThemeExpired(object sender, ThemeExpiredEventArgs e)
    {
        ThemeVariableRepository.RemoveFromCache(e.Cache, e.ThemeName);
    }
}