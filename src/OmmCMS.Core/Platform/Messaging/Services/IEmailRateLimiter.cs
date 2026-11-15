#nullable enable

namespace OmmCMS.Core.Messaging;

public interface IEmailRateLimiter
{
    int GetAllowedSendCount(int requestedCount);
}