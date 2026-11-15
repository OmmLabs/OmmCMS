using System.Collections.Frozen;
using OmmCMS.Core.Data;
using OmmCMS.Core.Rules;

namespace OmmCMS.Core.Checkout.Rules.Impl;

internal class CustomerAuthenticationRule : IRule<CartRuleContext>
{
    // INFO: ExternalAuthenticationRecord.ProviderSystemName always contains the old system name, even for new logins!
    private readonly static FrozenDictionary<string, string> _legacyAuthenticationNamesMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "OmmCMS.FacebookAuth", "OmmCMS.Facebook.Auth" },
        { "OmmCMS.TwitterAuth", "OmmCMS.Twitter.Auth" },
        { "OmmCMS.GoogleAuth", "OmmCMS.Google.Auth" }
    }
    .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private readonly SmartDbContext _db;

    public CustomerAuthenticationRule(SmartDbContext db)
    {
        _db = db;
    }

    public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
    {
        var customer = context.Customer;
        if (customer != null && !customer.IsSystemAccount)
        {
            await _db.LoadCollectionAsync(customer, x => x.ExternalAuthenticationRecords);

            // INFO: checking whether the authentication methods are also active is probably not necessary here.
            var authenticationMethods = customer.ExternalAuthenticationRecords
                .Select(x => _legacyAuthenticationNamesMap.Get(x.ProviderSystemName.EmptyNull()) ?? x.ProviderSystemName)
                .ToArray();

            var match = expression.HasListsMatch(authenticationMethods, comparer: StringComparer.InvariantCultureIgnoreCase);
            return match;
        }

        return false;
    }
}