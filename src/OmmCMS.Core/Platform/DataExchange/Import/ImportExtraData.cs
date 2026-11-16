namespace OmmCMS.Core.DataExchange.Import;

[Serializable]
public partial class ImportExtraData
{
    /// <summary>
    /// Gets or sets a value that indicates whether to update all records that match a key field value.
    /// </summary>
    public bool UpdateAllKeyFieldMatches { get; set; }
}
