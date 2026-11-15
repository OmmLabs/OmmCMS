using OmmCMS.Core.Configuration;

namespace OmmCMS.Apple.Auth;

public class AppleExternalAuthSettings : ISettings
{
    public string ClientId { get; set; }
    public string TeamId { get; set; }
    public string KeyId { get; set; }
    public string PrivateKey { get; set; }
}