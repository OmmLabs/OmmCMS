using Microsoft.Extensions.Configuration;

namespace Smartstore.Engine;

/// <summary>
/// Defines the opt-in CMS preview profile and the bundled modules excluded from it.
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
    /// Returns whether the CMS preview profile is enabled. Restart after changing the setting.
    /// </summary>
    public static bool IsEnabled(IConfiguration configuration)
        => bool.TryParse(configuration["OmmCMS:PreviewShell"], out var enabled) && enabled;

    /// <summary>
    /// Returns whether a bundled commerce or commerce-capable API module is excluded.
    /// </summary>
    public static bool IsExcludedModule(string systemName)
        => _excludedModules.Contains(systemName);
}
