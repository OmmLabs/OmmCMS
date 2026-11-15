using OmmCMS.Core.Rules;
using OmmCMS.Core.Web;

namespace OmmCMS.Core.Checkout.Rules.Impl;

internal class BrowserMinorVersionRule : IRule<CartRuleContext>
{
    private readonly IUserAgent _userAgent;

    public BrowserMinorVersionRule(IUserAgent userAgent)
    {
        _userAgent = userAgent;
    }

    public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
    {
        var match = false;

        if (_userAgent.Version is Version version)
        {
            match = expression.Operator.Match(version.Minor, expression.Value);
        }

        return Task.FromResult(match);
    }
}