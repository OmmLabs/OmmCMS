using Microsoft.Extensions.Configuration;

namespace Smartstore.Engine;

/// <summary>
/// Defines the CMS profile and the retired module identities blocked from loading.
/// </summary>
public static class OmmCmsMode
{
    private static readonly HashSet<string> _excludedModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "Smartstore.AmazonPay",
        "Smartstore.OfflinePayment",
        "Smartstore.PayPal",
        "Smartstore.Stripe",
        "Smartstore.Shipping",
        "Smartstore.ShippingByWeight",
        "Smartstore.Tax",
        "Smartstore.Google.MerchantCenter",
        "Smartstore.Clickatell",
        "Smartstore.WebApi"
    };

    /// <summary>
    /// Returns whether the CMS profile is enabled. The OmmCMS web host pins this setting to true.
    /// </summary>
    public static bool IsEnabled(IConfiguration configuration)
        => bool.TryParse(configuration["OmmCMS:PreviewShell"], out var enabled) && enabled;

    /// <summary>
    /// Returns whether a retired commerce or commerce-capable API module is excluded, including stale local builds.
    /// </summary>
    public static bool IsExcludedModule(string systemName)
        => _excludedModules.Contains(systemName);
}
