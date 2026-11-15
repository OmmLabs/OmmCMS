using OmmCMS.Core.Configuration;

namespace OmmCMS.Core.Checkout.Tax;

public class TaxSettings : ISettings
{
    /// <summary>
    /// Tax based on
    /// </summary>
    public TaxBasedOn TaxBasedOn { get; set; } = TaxBasedOn.BillingAddress;

    /// <summary>
    /// Tax display type
    /// </summary>
    public TaxDisplayType TaxDisplayType { get; set; } = TaxDisplayType.ExcludingTax;


    /// <summary>
    /// Gets or sets default address used for tax calculation
    /// </summary>
    public int DefaultTaxAddressId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to display tax suffix
    /// </summary>
    public bool DisplayTaxSuffix { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether each tax rate should be displayed on separate line (shopping cart page)
    /// </summary>
    public bool DisplayTaxRates { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether prices incude tax
    /// </summary>
    public bool PricesIncludeTax { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether customers are allowed to select tax display type
    /// </summary>
    public bool AllowCustomersToSelectTaxDisplayType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to hide zero tax
    /// </summary>
    public bool HideZeroTax { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to hide tax in order summary when prices are shown tax inclusive
    /// </summary>
    public bool HideTaxInOrderSummary { get; set; }

    [Obsolete("Use CatalogSettings.LegalInfoInLists instead.")]
    public bool ShowLegalHintsInProductList { get; set; } = true;

    [Obsolete("Use CatalogSettings.LegalInfoInProductDetail instead.")]
    public bool ShowLegalHintsInProductDetails { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show legal info in footer
    /// </summary>
    public bool ShowLegalHintsInFooter { get; set; } = true;

}
