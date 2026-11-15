using OmmCMS.Core.Messaging;

namespace OmmCMS.Core.Checkout.Orders;

public static class OrderMessageFactoryExtensions
{
    /// <summary>
    /// Sends an order placed notification to the store owner.
    /// </summary>
    public static Task<CreateMessageResult> SendOrderPlacedStoreOwnerNotificationAsync(this IMessageFactory factory, Order order, int languageId = 0)
    {
        Guard.NotNull(order);

        return factory.CreateMessageAsync(
            MessageContext.Create(MessageTemplateNames.OrderPlacedStoreOwner, languageId, order.StoreId),
            true,
            order,
            order.Customer);
    }

    /// <summary>
    /// Sends an order placed notification to the customer.
    /// </summary>
    public static Task<CreateMessageResult> SendOrderPlacedCustomerNotificationAsync(this IMessageFactory factory, Order order, int languageId = 0)
    {
        Guard.NotNull(order);

        return factory.CreateMessageAsync(
            MessageContext.Create(MessageTemplateNames.OrderPlacedCustomer, languageId, order.StoreId, order.Customer),
            true,
            order);
    }

    /// <summary>
    /// Sends an order completed notification to the customer.
    /// </summary>
    public static Task<CreateMessageResult> SendOrderCompletedCustomerNotificationAsync(this IMessageFactory factory, Order order, int languageId = 0)
    {
        Guard.NotNull(order, nameof(order));

        return factory.CreateMessageAsync(
            MessageContext.Create(MessageTemplateNames.OrderCompletedCustomer, languageId, order.StoreId, order.Customer),
            true,
            order);
    }

    /// <summary>
    /// Sends an order cancelled notification to the customer.
    /// </summary>
    public static Task<CreateMessageResult> SendOrderCancelledCustomerNotificationAsync(this IMessageFactory factory, Order order, int languageId = 0)
    {
        Guard.NotNull(order);

        return factory.CreateMessageAsync(
            MessageContext.Create(MessageTemplateNames.OrderCancelledCustomer, languageId, order.StoreId, order.Customer),
            true,
            order);
    }

}
