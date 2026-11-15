namespace OmmCMS.Core.DataExchange;

/// <summary>
/// Contains data projected onto an export.
/// </summary>
[Serializable]
public class ExportProjection
{
    #region All entity types

    /// <summary>
    /// Store identifier.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// The language to be applied to the export.
    /// </summary>
    public int? LanguageId { get; set; }

    /// <summary>
    /// The currency to be applied to the export.
    /// </summary>
    public int? CurrencyId { get; set; }

    /// <summary>
    /// Customer identifier.
    /// </summary>
    public int? CustomerId { get; set; }

    #endregion
}
