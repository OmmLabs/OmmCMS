using OmmCMS.Core.DataExchange;
using OmmCMS.Engine.Modularity;

namespace OmmCMS.Admin.Models.Export;

public class ExportPreviewModel : EntityModelBase
{
    public string Name { get; set; }
    public ExportEntityType EntityType { get; set; }

    public ModuleIcon Icon { get; set; }
    public bool LogFileExists { get; set; }
    public bool UsernamesEnabled { get; set; }
}

[LocalizedDisplay("Admin.Customers.Customers.Fields.")]
public class ExportPreviewCustomerModel : EntityModelBase
{
    public bool UsernamesEnabled { get; set; }

    [LocalizedDisplay("*Username")]
    public string Username { get; set; }

    [LocalizedDisplay("*FullName")]
    public string FullName { get; set; }

    [LocalizedDisplay("*Email")]
    public string Email { get; set; }

    [LocalizedDisplay("*CustomerRoles")]
    public string CustomerRoleNames { get; set; }

    [LocalizedDisplay("*Active")]
    public bool Active { get; set; }

    [LocalizedDisplay("Common.CreatedOn")]
    public DateTime CreatedOn { get; set; }

    [LocalizedDisplay("*LastActivityDate")]
    public DateTime LastActivityDate { get; set; }
}

[LocalizedDisplay("Admin.Promotions.NewsletterSubscriptions.Fields.")]
public class ExportPreviewNewsletterSubscriptionModel : EntityModelBase
{
    [LocalizedDisplay("*Email")]
    public string Email { get; set; }

    [LocalizedDisplay("*Active")]
    public bool Active { get; set; }

    [LocalizedDisplay("Common.CreatedOn")]
    public DateTime CreatedOn { get; set; }

    [LocalizedDisplay("Admin.Common.Store")]
    public string StoreName { get; set; }
}
