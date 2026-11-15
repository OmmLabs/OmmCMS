using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OmmCMS.Core.Checkout.Cart;
using OmmCMS.Core.Checkout.Rules;
using OmmCMS.Core.Checkout.Rules.Impl;
using OmmCMS.Core.Checkout.Shipping;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Rules;

namespace OmmCMS.Core.Tests.Checkout.Rules;

[TestFixture]
public class CartWeightRuleTests
{
    [Test]
    public async Task Uses_uncached_cart_weight()
    {
        var cart = new ShoppingCart(new Customer { Id = 1 }, 1, []);
        var shippingService = new Mock<IShippingService>();
        shippingService
            .Setup(x => x.GetCartTotalWeightAsync(cart, true, false))
            .ReturnsAsync(10m);

        var rule = new CartWeightRule(shippingService.Object);
        var context = new CartRuleContext(() => 1) { ShoppingCart = cart };
        var expression = new RuleExpression
        {
            Operator = RuleOperator.GreaterThan,
            Value = 5m
        };

        var result = await rule.MatchAsync(context, expression);

        Assert.That(result, Is.True);
        shippingService.Verify(x => x.GetCartTotalWeightAsync(cart, true, false), Times.Once);
    }
}