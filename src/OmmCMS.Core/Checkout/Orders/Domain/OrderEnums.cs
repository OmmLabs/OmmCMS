namespace OmmCMS.Core.Checkout.Orders;

/// <summary>
/// Represents requirements of a standard checkout.
/// </summary>
[Flags]
public enum CheckoutRequirements
{
    BillingAddress = 1 << 0,
    Shipping = 1 << 1,
    Payment = 1 << 2,

    All = BillingAddress | Shipping | Payment
}
/// <summary>
/// Represents an order status.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Pending
    /// </summary>
    Pending = 10,

    /// <summary>
    /// Processing
    /// </summary>
    Processing = 20,

    /// <summary>
    /// Complete
    /// </summary>
    Complete = 30,

    /// <summary>
    /// Cancelled
    /// </summary>
    Cancelled = 40
}
