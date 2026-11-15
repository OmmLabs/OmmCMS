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
    /// The system default id of PriceLabel entity for product compare prices.
    /// Takes effect when a product does not override the ComparePrice label.
    /// </summary>
    public int? DefaultComparePriceLabelId { get; set; }

    /// <summary>
    /// The system default id of PriceLabel entity to use for the crossed out regular price.
    /// Takes effect when there is an offer or a discount has been applied to a product.
    /// </summary>
    public int? DefaultRegularPriceLabelId { get; set; }

    /// <summary>
    /// If TRUE, the MSRP will be displayed in product detail even if there is already an offer or a discount.
    /// In this case the MSRP will appear as another crossed out price alongside the discounted price.
    /// </summary>
    public bool AlwaysDisplayRetailPrice { get; set; } = true;

    /// <summary>
    /// <c>true</c> to display the compare price label's short name in product lists.
    /// </summary>
    public bool ShowPriceLabelInLists { get; set; } = true;

    /// <summary>
    /// If TRUE, displays price saving even if the reference price is the retail price only.
    /// </summary>
    public bool ShowRetailPriceSaving { get; set; } = true;

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
