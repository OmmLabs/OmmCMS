using System.ComponentModel.DataAnnotations;
using FluentValidation;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Rules;
using OmmCMS.Web.Models.Customers;

namespace OmmCMS.Admin.Models.Customers;

[LocalizedDisplay("Admin.Customers.CustomerRoles.Fields.")]
public class CustomerRoleModel : TabbableModel
{
    [LocalizedDisplay("*Name")]
    public string Name { get; set; }

    [LocalizedDisplay("*Active")]
    public bool Active { get; set; }

    [LocalizedDisplay("*IsSystemRole")]
    public bool IsSystemRole { get; set; }

    [LocalizedDisplay("*SystemName")]
    public string SystemName { get; set; }

    [UIHint("RuleSets")]
    [AdditionalMetadata("multiple", true)]
    [AdditionalMetadata("scope", RuleScope.Customer)]
    [LocalizedDisplay("Admin.Customers.CustomerRoles.AutomatedAssignmentRules")]
    public int[] SelectedRuleSetIds { get; set; }

    public string EditUrl { get; set; }

    public CustomerSearchModel CustomerSearch { get; set; } = new();
}

public partial class CustomerRoleValidator : AbstractValidator<CustomerRoleModel>
{
    public CustomerRoleValidator(Localizer _)
    {
        RuleFor(x => x.Name).NotNull();

    }
}