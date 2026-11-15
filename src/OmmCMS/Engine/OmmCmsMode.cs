using Microsoft.Extensions.Configuration;

namespace OmmCMS.Engine;

/// <summary>
/// Defines the CMS profile and the retired module identities blocked from loading.
/// </summary>
public static class OmmCmsMode
{
    private static readonly HashSet<string> _excludedModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "OmmCMS.AmazonPay",
        "OmmCMS.OfflinePayment",
        "OmmCMS.PayPal",
        "OmmCMS.Stripe",
        "OmmCMS.Shipping",
        "OmmCMS.ShippingByWeight",
        "OmmCMS.Tax",
        "OmmCMS.Google.MerchantCenter",
        "OmmCMS.Clickatell",
        "OmmCMS.WebApi"
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
