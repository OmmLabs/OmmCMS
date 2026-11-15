
namespace OmmCMS.Admin.Models.Export;

[LocalizedDisplay("Admin.DataExchange.Export.Projection.")]
public class ExportProjectionModel
{
    #region All entity types

    [LocalizedDisplay("*StoreId")]
    public int? StoreId { get; set; }

    [LocalizedDisplay("*LanguageId")]
    public int? LanguageId { get; set; }

    [LocalizedDisplay("*CurrencyId")]
    public int? CurrencyId { get; set; }

    [LocalizedDisplay("*CustomerId")]
    [AdditionalMetadata("invariant", true)]
    public int? CustomerId { get; set; }

    #endregion

    #region Product

    [LocalizedDisplay("*DescriptionMerging")]
    public int DescriptionMergingId { get; set; }

    [LocalizedDisplay("*DescriptionToPlainText")]
    public bool DescriptionToPlainText { get; set; }

    [LocalizedDisplay("*AppendDescriptionText")]
    public string[] AppendDescriptionText { get; set; }

    [LocalizedDisplay("*RemoveCriticalCharacters")]
    public bool RemoveCriticalCharacters { get; set; }

    [LocalizedDisplay("*CriticalCharacters")]
    public string[] CriticalCharacters { get; set; }

    [LocalizedDisplay("*Brand")]
    public string Brand { get; set; }

    [LocalizedDisplay("*NumberOfPictures")]
    public int? NumberOfPictures { get; set; }

    [LocalizedDisplay("*PictureSize")]
    public int PictureSize { get; set; }

    [LocalizedDisplay("*ShippingTime")]
    public string ShippingTime { get; set; }

    [LocalizedDisplay("*AttributeCombinationAsProduct")]
    public bool AttributeCombinationAsProduct { get; set; }

    [LocalizedDisplay("*AttributeCombinationValueMerging")]
    public int AttributeCombinationValueMergingId { get; set; }

    [LocalizedDisplay("*NoGroupedProducts")]
    public bool NoGroupedProducts { get; set; }

    [LocalizedDisplay("*OnlyIndividuallyVisibleAssociated")]
    public bool OnlyIndividuallyVisibleAssociated { get; set; }

    #endregion
}
