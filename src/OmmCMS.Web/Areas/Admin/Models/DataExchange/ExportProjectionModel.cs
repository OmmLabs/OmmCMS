
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
}
