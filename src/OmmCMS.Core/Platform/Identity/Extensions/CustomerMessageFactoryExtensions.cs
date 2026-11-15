using OmmCMS.Core.Messaging;

namespace OmmCMS.Core.Identity;

public static partial class CustomerMessageFactoryExtensions
{
    /// <summary>
    /// Sends 'New customer' notification message to a store owner.
    /// </summary>
    public static Task<CreateMessageResult> SendCustomerRegisteredNotificationMessageAsync(this IMessageFactory factory, Customer customer, int languageId = 0)
    {
        Guard.NotNull(customer, nameof(customer));
        return factory.CreateMessageAsync(MessageContext.Create(MessageTemplateNames.CustomerRegistered, languageId, customer: customer), true);
    }

    /// <summary>
    /// Sends a welcome message to a customer.
    /// </summary>
    public static Task<CreateMessageResult> SendCustomerWelcomeMessageAsync(this IMessageFactory factory, Customer customer, int languageId = 0)
    {
        Guard.NotNull(customer, nameof(customer));
        return factory.CreateMessageAsync(MessageContext.Create(MessageTemplateNames.CustomerWelcome, languageId, customer: customer), true);
    }

    /// <summary>
    /// Sends an email validation message to a customer.
    /// </summary>
    public static Task<CreateMessageResult> SendCustomerEmailValidationMessageAsync(this IMessageFactory factory, Customer customer, int languageId = 0)
    {
        Guard.NotNull(customer, nameof(customer));
        return factory.CreateMessageAsync(MessageContext.Create(MessageTemplateNames.CustomerEmailValidation, languageId, customer: customer), true);
    }

    /// <summary>
    /// Sends password recovery message to a customer.
    /// </summary>
    public static Task<CreateMessageResult> SendCustomerPasswordRecoveryMessageAsync(this IMessageFactory factory, Customer customer, int languageId = 0)
    {
        Guard.NotNull(customer, nameof(customer));
        return factory.CreateMessageAsync(MessageContext.Create(MessageTemplateNames.CustomerPasswordRecovery, languageId, customer: customer), true);
    }
}
