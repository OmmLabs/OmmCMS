using OmmCMS.Core.Checkout.Orders;
using OmmCMS.Core.Checkout.Orders.Events;
using OmmCMS.Events;

namespace OmmCMS;

public static class OrderEventPublisherExtensions
{
    /// <summary>
    /// Publishes the order updated event.
    /// </summary>
    /// <param name="eventPublisher">The event publisher.</param>
    /// <param name="order">The order instance.</param>
    public static Task PublishOrderUpdatedAsync(this IEventPublisher eventPublisher, Order order)
    {
        return order != null
            ? eventPublisher.PublishAsync(new OrderUpdatedEvent(order))
            : Task.CompletedTask;
    }
}
