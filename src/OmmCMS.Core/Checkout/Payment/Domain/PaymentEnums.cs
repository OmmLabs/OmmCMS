namespace OmmCMS.Core.Checkout.Payment;

/// <summary>
/// Represents a payment status.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// The initial payment status if no further status information is available yet.
    /// </summary>
    Pending = 10,

    /// <summary>
    /// The payment has been authorized (but not captured) by the payment provider.
    /// Usually this means that the payment amount is reserved for later capturing.
    /// </summary>
    Authorized = 20,

    /// <summary>
    /// The payment has been captured against the payment gateway.
    /// It does not necessarily mean that the paid amount has been credited to the merchant's account.
    /// </summary>
    Paid = 30,

    /// <summary>
    /// The paid amount has been partially refunded.
    /// </summary>
    PartiallyRefunded = 35,

    /// <summary>
    /// The paid amount has been fully refunded.
    /// </summary>
    Refunded = 40,

    /// <summary>
    /// The payment has been cancelled.
    /// </summary>
    Voided = 50,
}
