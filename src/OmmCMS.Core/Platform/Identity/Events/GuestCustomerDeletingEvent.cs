using OmmCMS.Events;

namespace OmmCMS.Core.Identity;

/// <summary>
/// An event message that will be published before guest customers are deleted.
/// </summary>
public class GuestCustomerDeletingEvent : IEventMessage
{
    public GuestCustomerDeletingEvent(
        DateTime? registrationFrom,
        DateTime? registrationTo)
    {
        RegistrationFrom = registrationFrom;
        RegistrationTo = registrationTo;
    }

    /// <summary>
    /// Customer registration from. <c>null</c> to ignore.
    /// Already included in <see cref="Query"/>.
    /// </summary>
    public DateTime? RegistrationFrom { get; }

    /// <summary>
    /// Customer registration to. <c>null</c> to ignore.
    /// Already included in <see cref="Query"/>.
    /// </summary>
    public DateTime? RegistrationTo { get; }

    /// <summary>
    /// Gets or sets the query used for deleting guest customers.
    /// </summary>
    public IQueryable<Customer> Query { get; set; }
}