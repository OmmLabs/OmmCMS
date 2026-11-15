using OmmCMS.Events;

namespace OmmCMS.Core.Checkout.Orders.Events;

public class OrderUpdatedEvent(Order order) : IEventMessage
{
    public Order Order { get; init; } = Guard.NotNull(order);
}
