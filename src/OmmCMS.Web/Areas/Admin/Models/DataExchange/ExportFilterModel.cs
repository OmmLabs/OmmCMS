using System.ComponentModel.DataAnnotations;

namespace OmmCMS.Admin.Models.Export;

[LocalizedDisplay("Admin.DataExchange.Export.Filter.")]
public class ExportFilterModel
{
    #region All entity types

    [UIHint("Stores")]
    [LocalizedDisplay("*StoreId")]
    public int? StoreId { get; set; }

    [LocalizedDisplay("*CreatedFrom")]
    [AdditionalMetadata("pickTime", true)]
    public DateTime? CreatedFrom { get; set; }

    [LocalizedDisplay("*CreatedTo")]
    [AdditionalMetadata("pickTime", true)]
    public DateTime? CreatedTo { get; set; }

    [UIHint("CustomerRoles")]
    [AdditionalMetadata("multiple", true)]
    [LocalizedDisplay("*CustomerRoleIds")]
    public int[] CustomerRoleIds { get; set; }

    #endregion

    #region Customer

    [LocalizedDisplay("*IsActiveCustomer")]
    public bool? IsActiveCustomer { get; set; }

    [LocalizedDisplay("Address.Fields.Country")]
    public int[] ContactCountryIds { get; set; }

    [LocalizedDisplay("*LastActivityFrom")]
    [AdditionalMetadata("pickTime", true)]
    public DateTime? LastActivityFrom { get; set; }

    [LocalizedDisplay("*LastActivityTo")]
    [AdditionalMetadata("pickTime", true)]
    public DateTime? LastActivityTo { get; set; }

    #endregion

    #region Newsletter Subscription

    [LocalizedDisplay("*IsActiveSubscriber")]
    public bool? IsActiveSubscriber { get; set; }

    [LocalizedDisplay("*WorkingLanguageId")]
    public int? WorkingLanguageId { get; set; }

    #endregion

    #region Shopping Cart Items

    #endregion
}
