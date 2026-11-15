using Autofac;
using OmmCMS.Core.Stores;

namespace OmmCMS.Core.Bootstrapping;

public sealed class StoresModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<StoreContext>().As<IStoreContext>().InstancePerLifetimeScope();
        builder.RegisterType<StoreMappingService>().As<IStoreMappingService>().InstancePerLifetimeScope();
    }
}