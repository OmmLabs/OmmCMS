using OmmCMS.Core.Catalog.Products;
using OmmCMS.Core.Configuration;
using OmmCMS.Core.Localization;

namespace OmmCMS.Core.Checkout.Orders;

public class OrderSettings : ISettings
{
    /// <summary>
    /// Gets or sets a value indicating whether customer can make a re-order.
    /// </summary>
    public bool IsReOrderAllowed { get; set; } = true;

    /// <summary>
    /// Gets or sets a minimum order total amount.
    /// </summary>
    public decimal? OrderTotalMinimum { get; set; }

    /// <summary>
    /// Gets or sets a maximum order total amount
    /// </summary>
    public decimal? OrderTotalMaximum { get; set; }

    /// <summary>
    /// Gets or sets a value indicating how customer group restrictions are applied with each other.
    /// <c>true</c>: lowest possible total amount span gets applied. <c>false</c>: highest possible total amount span gets applied.
    /// </summary>
    public bool MultipleOrderTotalRestrictionsExpandRange { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow anonymous checkout.
    /// </summary>
    public bool AnonymousCheckoutAllowed { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether "Order completed" page should be skipped-
    /// </summary>
    public bool DisableOrderCompletedPage { get; set; }

    /// <summary>
    /// Gets or sets an order placement interval in seconds (prevent 2 orders being placed within an X seconds time frame).
    /// </summary>
    public int MinimumOrderPlacementInterval { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to display all orders of all stores to a customer.
    /// </summary>
    public bool DisplayOrdersOfAllStores { get; set; }

    /// <summary>
    /// Gets or sets the page size of the order list.
    /// </summary>
    public int OrderListPageSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum order age in days up to which to create and send messages.
    /// Set to 0 to always send messages.
    /// </summary>
    public int MaxMessageOrderAgeInDays { get; set; } = 180;
}
