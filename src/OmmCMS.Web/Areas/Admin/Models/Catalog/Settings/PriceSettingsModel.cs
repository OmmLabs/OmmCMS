using System.ComponentModel.DataAnnotations;
using OmmCMS.Core.Catalog.Pricing;

namespace OmmCMS.Admin.Models.Catalog;

[LocalizedDisplay("Admin.Configuration.Settings.Price.")]
public class PriceSettingsModel : ModelBase
{
    #region Moved from CatalogSettings

    [LocalizedDisplay("*ShowBasePriceInProductLists")]
    public bool ShowBasePriceInProductLists { get; set; }

    [LocalizedDisplay("*ShowVariantCombinationPriceAdjustment")]
    public bool ShowVariantCombinationPriceAdjustment { get; set; }

    [LocalizedDisplay("*ShowLoginForPriceNote")]
    public bool ShowLoginForPriceNote { get; set; }

    [LocalizedDisplay("*BundleItemShowBasePrice")]
    public bool BundleItemShowBasePrice { get; set; }

    [LocalizedDisplay("*ShowSavingBadgeInLists")]
    public bool ShowSavingBadgeInLists { get; set; }

    [LocalizedDisplay("*PriceDisplayType")]
    public PriceDisplayType PriceDisplayType { get; set; }

    [LocalizedDisplay("*DisplayTextForZeroPrices")]
    public bool DisplayTextForZeroPrices { get; set; }

    #endregion

    #region New

    [LocalizedDisplay("*DefaultComparePriceLabelId")]
    public int? DefaultComparePriceLabelId { get; set; }

    [LocalizedDisplay("*DefaultRegularPriceLabelId")]
    public int? DefaultRegularPriceLabelId { get; set; }

    [LocalizedDisplay("*AlwaysDisplayRetailPrice")]
    public bool AlwaysDisplayRetailPrice { get; set; }

    [LocalizedDisplay("*ShowPriceLabelInLists")]
    public bool ShowPriceLabelInLists { get; set; }

    [LocalizedDisplay("*ShowRetailPriceSaving")]
    public bool ShowRetailPriceSaving { get; set; }

    [LocalizedDisplay("*ApplyPriceRangeFormatInProductDetails")]
    public bool ApplyPriceRangeFormatInProductDetails { get; set; }

    [LocalizedDisplay("*ShowRequiredProductPricesWithMainProduct")]
    public bool ShowRequiredProductPricesWithMainProduct { get; set; }

    [LocalizedDisplay("*RulesCalculateIncludingTax")]
    public bool? RulesCalculateIncludingTax { get; set; }

    #endregion
}
