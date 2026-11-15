using FluentValidation;
using OmmCMS.Admin.Components;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Security;
using OmmCMS.Core.Widgets;
using OmmCMS.Core.Widgets.Dashboard;
using OmmCMS.Engine.Modularity;
using OmmCMS.Web.Modelling;

namespace OmmCMS.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the widget showing recently recorded contact messages.
/// </summary>
[SystemName(SystemName)]
public sealed class ContactRequestsDashboardWidget(
    IPermissionService permissions,
    IValidator<ContactRequestsDashboardWidgetSettings> validator) : DashboardViewComponentWidget<DashboardContactRequestsViewComponent, ContactRequestsDashboardWidgetSettings>(
        permissions,
        DashboardContactRequestsViewComponent.RequiredPermission,
        validator)
{
    /// <summary>
    /// Identifies the contact requests dashboard widget type.
    /// </summary>
    public const string SystemName = "OmmCMS.Admin.Dashboard.ContactRequests";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.Dashboard.ContactRequests"))
    {
        Description = ResolvableText.Resource("Admin.Dashboard.ContactRequests.Description"),
        Group = KnownDashboardWidgetGroups.Customers,
        IconName = "mail",
        Order = 575,
        Policy = new() { AllowConfigure = true },
        DefaultSize = new DashboardWidgetSize(6, 1),
        MinSize = new DashboardWidgetSize(4, 1),
        MaxSize = new DashboardWidgetSize(12)
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;

    /// <inheritdoc />
    public override Task<Widget> CreateConfigurationWidgetAsync(DashboardWidgetContext context, DashboardWidgetInstance instance)
        => Task.FromResult<Widget>(new PartialViewWidget("Components/DashboardContactRequests/Configure", LoadSettings(instance.Settings)));

    /// <inheritdoc />
    protected override object GetWidgetArguments(DashboardWidgetContext context, DashboardWidgetInstance instance, ContactRequestsDashboardWidgetSettings settings)
        => new { count = settings.Count };
}

/// <summary>Contains the settings of the ContactRequests dashboard widget.</summary>
[LocalizedDisplay("Admin.Dashboard.Widget.")]
public class ContactRequestsDashboardWidgetSettings
{
    /// <summary>Gets or sets the maximum number of items to display.</summary>
    [LocalizedDisplay("*Count")]
    public int Count { get; set; } = 5;
}

/// <summary>Validates settings of the ContactRequests dashboard widget.</summary>
public class ContactRequestsDashboardWidgetValidator : AbstractValidator<ContactRequestsDashboardWidgetSettings>
{
    public ContactRequestsDashboardWidgetValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 50)
            .WithMessage("The number of displayed items must be between 1 and 50.");
    }
}
