namespace OmmCMS.Core.DataExchange;

/// <summary>
/// Contains data to filter entities during an export.
/// </summary>
[Serializable]
public class ExportFilter
{
    /// <summary>
    /// Store identifier. 0 to load all records.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Entity created from.
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Entity created to.
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    #region Customer

    /// <summary>
    /// Filter by active or inactive customers.
    /// </summary>
    public bool? IsActiveCustomer { get; set; }

    /// <summary>
    /// Identifiers of customer roles.
    /// </summary>
    public int[] CustomerRoleIds { get; set; }

    /// <summary>
    /// Filter by contact country identifiers.
    /// </summary>
    public int[] ContactCountryIds { get; set; }

    /// <summary>
    /// Filter by last activity date from.
    /// </summary>
    public DateTime? LastActivityFrom { get; set; }

    /// <summary>
    /// Filter by last activity date to.
    /// </summary>
    public DateTime? LastActivityTo { get; set; }

    #endregion

    #region Newsletter Subscription

    /// <summary>
    /// Filter by active or inactive subscriber.
    /// </summary>
    public bool? IsActiveSubscriber { get; set; }

    /// <summary>
		/// Filter by language.
		/// </summary>
    public int? WorkingLanguageId { get; set; }

    #endregion
}
