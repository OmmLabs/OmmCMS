using System.Reflection;
using Autofac;
using OmmCMS.Engine;
using OmmCMS.Engine.Modularity;

namespace OmmCMS.Events;

public class ConsumerResolver : IConsumerResolver
{
    private readonly IApplicationContext _appContext;

    public ConsumerResolver(IApplicationContext appContext)
    {
        _appContext = appContext;
    }


    public virtual IConsumer Resolve(ConsumerDescriptor descriptor)
    {
        var scope = EngineContext.Current.Scope;

        if (descriptor.ModuleDescriptor != null)
        {
            if (!_appContext.IsInstalled)
            {
                return null;
            }

            if (!scope.Resolve<IModuleConstraint>().Matches(descriptor.ModuleDescriptor, null))
            {
                return null;
            }
        }

        return scope.ResolveKeyed<IConsumer>(descriptor.ContainerType);

    }

    public virtual object ResolveParameter(ParameterInfo p, IComponentContext c = null)
    {
        return (c ?? EngineContext.Current.Scope.RequestContainer).Resolve(p.ParameterType);
    }
}