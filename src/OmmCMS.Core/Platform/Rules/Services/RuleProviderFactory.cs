using Autofac;
using OmmCMS.Core.Identity.Rules;

namespace OmmCMS.Core.Rules;

public class RuleProviderFactory : IRuleProviderFactory
{
    private readonly ILifetimeScope _scope;

    public RuleProviderFactory(ILifetimeScope lifetimeScope)
    {
        _scope = lifetimeScope;
    }

    public virtual IRuleProvider GetProvider(RuleScope scope, object context = null)
    {
        switch (scope)
        {
            case RuleScope.Customer:
                return _scope.Resolve<ITargetGroupService>();
            default:
                throw new ArgumentOutOfRangeException(nameof(scope), $"Cannot get rule provider for scope {scope}. There is no known provider for this scope.");
        }
    }
}
