using OmmCMS.Core.Identity;
using OmmCMS.Events;

namespace OmmCMS.Core.Checkout.Cart.Events;

/// <summary>
/// Represents a rendering order totals event
/// </summary>
public class RenderingOrderTotalsEvent : IEventMessage
{
    /// <summary>
    /// Gets the customer
    /// </summary>
    public Customer Customer { get; set; }

    /// <summary>
    /// Gets the store id
    /// </summary>
    public int? StoreId { get; set; }
}