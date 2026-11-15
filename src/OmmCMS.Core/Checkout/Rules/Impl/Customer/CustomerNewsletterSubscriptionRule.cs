using OmmCMS.Core.Data;
using OmmCMS.Core.Rules;

namespace OmmCMS.Core.Checkout.Rules.Impl;

internal class CustomerNewsletterSubscriptionRule(SmartDbContext db) : IRule<CartRuleContext>
{
    private readonly SmartDbContext _db = db;

    public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
    {
        if (context.Customer.Email.IsEmpty())
        {
            return false;
        }

        return await _db.NewsletterSubscriptions
            .ApplyMailAddressFilter(context.Customer.Email, context.Store.Id)
            .Where(x => x.Active)
            .AnyAsync();
    }
}