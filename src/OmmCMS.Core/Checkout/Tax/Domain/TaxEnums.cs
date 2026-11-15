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

/// <summary>
/// Represents the VAT number status.
/// </summary>
public enum VatNumberStatus
{
    /// <summary>
    /// Unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Empty.
    /// </summary>
    Empty = 10,

    /// <summary>
    /// Valid.
    /// </summary>
    Valid = 20,

    /// <summary>
    /// Invalid.
    /// </summary>
    Invalid = 30,

    /// <summary>
    /// This indicates that the service used to check the status is temporarily unavailable,
    /// possibly due to maintenance or an overload.
    /// </summary>
    ServiceUnavailable
}
