using OmmCMS.Events;

namespace OmmCMS.Core.Identity;

/// <summary>
/// An event message, which will be published after customer has logged in.
/// </summary>
public class CustomerSignedInEvent : IEventMessage
{
    public Customer Customer { get; set; }
}