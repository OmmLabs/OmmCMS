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

    [LocalizedDisplay("*ApplyPriceRangeFormatInProductDetails")]
    public bool ApplyPriceRangeFormatInProductDetails { get; set; }

    [LocalizedDisplay("*ShowRequiredProductPricesWithMainProduct")]
    public bool ShowRequiredProductPricesWithMainProduct { get; set; }

    [LocalizedDisplay("*RulesCalculateIncludingTax")]
    public bool? RulesCalculateIncludingTax { get; set; }

    #endregion
}
