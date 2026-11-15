using OmmCMS.Admin.Models.Modularity;
using OmmCMS.Core.Checkout.Payment;

namespace OmmCMS.Admin.Models.Payments;

[LocalizedDisplay("Admin.Configuration.Payment.Methods.Fields.")]
public class PaymentMethodModel : ActivatableProviderModel
{
    [LocalizedDisplay("*SupportCapture")]
    public bool SupportCapture { get; set; }

    [LocalizedDisplay("*SupportPartiallyRefund")]
    public bool SupportPartiallyRefund { get; set; }

    [LocalizedDisplay("*SupportRefund")]
    public bool SupportRefund { get; set; }

    [LocalizedDisplay("*SupportVoid")]
    public bool SupportVoid { get; set; }
}
