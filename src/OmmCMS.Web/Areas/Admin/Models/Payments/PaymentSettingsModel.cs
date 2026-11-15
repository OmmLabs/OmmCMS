namespace OmmCMS.Admin.Models.Payments;

[LocalizedDisplay("Admin.Configuration.Settings.Payment.")]
public class PaymentSettingsModel : ModelBase
{
    [LocalizedDisplay("*ProductDetailPaymentMethodSystemNames")]
    public string[] ProductDetailPaymentMethodSystemNames { get; set; }

    [LocalizedDisplay("*DisplayPaymentMethodIcons")]
    public bool DisplayPaymentMethodIcons { get; set; }

    [LocalizedDisplay("*SkipPaymentSelectionIfSingleOption")]
    public bool SkipPaymentSelectionIfSingleOption { get; set; }
}
