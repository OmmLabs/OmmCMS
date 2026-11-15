using Autofac;
using Microsoft.Extensions.Caching.Memory;
using OmmCMS.Core.Theming;

namespace OmmCMS.Core.Bootstrapping;

public sealed class ThemesModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(x => new DefaultThemeRegistry(x.Resolve<IApplicationContext>(), x.Resolve<IMemoryCache>(), true))
            .As<IThemeRegistry>()
            .SingleInstance();
    }
}