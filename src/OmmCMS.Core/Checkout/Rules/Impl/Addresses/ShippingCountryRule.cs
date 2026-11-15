using OmmCMS.Core.Rules;

namespace OmmCMS.Core.Checkout.Rules.Impl;

internal class ShippingCountryRule : IRule<CartRuleContext>
{
    public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
    {
        var match = expression.HasListMatch(context.Customer?.ShippingAddress?.CountryId ?? 0);
        return Task.FromResult(match);
    }
}