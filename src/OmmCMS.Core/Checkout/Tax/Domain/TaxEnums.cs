namespace OmmCMS.Core.Checkout.Tax;

/// <summary>
/// Represents the pricing target for tax formatting.
/// </summary>
public enum PricingTarget
{
    Product
}

/// <summary>
/// Represents the tax display type.
/// </summary>
public enum TaxDisplayType
{
    /// <summary>
    /// Including tax.
    /// </summary>
    IncludingTax = 0,

    /// <summary>
    /// Excluding tax.
    /// </summary>
    ExcludingTax = 10
}
/// <summary>
/// Represents the tax base.
/// </summary>
public enum TaxBasedOn
{
    /// <summary>
    /// Billing address.
    /// </summary>
    BillingAddress = 1,

    /// <summary>
    /// Shipping address.
    /// </summary>
    ShippingAddress = 2,

    /// <summary>
    /// Default address.
    /// </summary>
    DefaultAddress = 3
}
