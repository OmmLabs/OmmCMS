using OmmCMS.Core.Catalog.Discounts;
using OmmCMS.Core.Checkout.Shipping;
using OmmCMS.Core.Common;

namespace OmmCMS.Core.Checkout.Cart;

/// <summary>
/// Represents a calculated shopping cart shipping total.
/// </summary>
public partial class ShoppingCartShippingTotal
{
    public static implicit operator Money?(ShoppingCartShippingTotal obj)
        => obj.ShippingTotal;

    /// <summary>
    /// Cart shipping total in the primary currency.
    /// </summary>
    public Money? ShippingTotal { get; init; }

    /// <summary>
    /// The applied shipping option.
    /// </summary>
    public ShippingOption Option { get; init; }

    /// <summary>
    /// Applied discount.
    /// </summary>
    public Discount AppliedDiscount { get; init; }

    /// <summary>
    /// Tax rate.
    /// </summary>
    public decimal TaxRate { get; init; }

    /// <summary>
    /// Returns the rounded and formatted <see cref="ShippingTotal"/>.
    /// </summary>
    public override string ToString()
        => (ShippingTotal ?? Money.Zero).ToString();
}