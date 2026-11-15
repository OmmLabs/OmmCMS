using OmmCMS.Core.Configuration;
using OmmCMS.Core.Localization;

namespace OmmCMS.Core.Catalog.Pricing;

public class PriceSettings : ISettings
{
    #region Moved from CatalogSettings

    /// <summary>
    /// Gets or sets a value indicating whether to display the base price of a product
    /// </summary>
    public bool ShowBasePriceInProductLists { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to display price adjustment of a product variant combination
    /// </summary>
    public bool ShowVariantCombinationPriceAdjustment { get; set; } = true;

    /// <summary>
    /// Indicates whether to show a login note if the user is not authorized to see prices.
    /// </summary>
    public bool ShowLoginForPriceNote { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether base price should be rendered for bundle items
    /// </summary>
    public bool BundleItemShowBasePrice { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show savings badge in product lists
    /// </summary>
    public bool ShowSavingBadgeInLists { get; set; } = true;

    /// <summary>
    /// Gets or sets the price display type for prices in product lists
    /// </summary>
    public PriceDisplayType PriceDisplayType { get; set; }

    /// <summary>
    /// Displays a textual resources instead of the decimal value when prices are 0
    /// </summary>
    public bool DisplayTextForZeroPrices { get; set; }

    #endregion

    #region New

    /// <summary>
    /// <c>true</c> to initially apply the price range format (e.g. "from {0}") in product details.
    /// </summary>
    public bool ApplyPriceRangeFormatInProductDetails { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the prices of automatically added required products 
    /// are displayed discreetly below the price of the main product.
    /// </summary>
    public bool ShowRequiredProductPricesWithMainProduct { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether rule builder rules calculate prices including or excluding tax.
    /// If <c>null</c> (default), it will be obtained via <see cref="IWorkContext.TaxDisplayType" />.
    /// </summary>
    public bool? RulesCalculateIncludingTax { get; set; }

    #endregion
}
