using OmmCMS.Core.Checkout.Cart;
using OmmCMS.Core.Rules;

namespace OmmCMS.Core.Checkout.Rules.Impl;

internal class CartProductCountRule : IRule<CartRuleContext>
{
    public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
    {
        var productCount = context.ShoppingCart.GetTotalQuantity();

        return Task.FromResult(expression.Operator.Match(productCount, expression.Value));
    }
}