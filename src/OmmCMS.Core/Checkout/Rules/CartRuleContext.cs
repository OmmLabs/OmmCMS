using OmmCMS.Core.Identity;
using OmmCMS.Core.Stores;

namespace OmmCMS.Core.Checkout.Rules;

public class CartRuleContext
{
    private readonly Func<object> _sessionKeyBuilder;
    private object _sessionKey;

    internal CartRuleContext(Func<object> sessionKeyBuilder)
    {
        _sessionKeyBuilder = sessionKeyBuilder;
    }

    public IWorkContext WorkContext { get; init; }

    public Customer Customer { get; set; }
    public Store Store { get; set; }

    public object SessionKey
        => _sessionKey ??= _sessionKeyBuilder?.Invoke() ?? 0;
}
