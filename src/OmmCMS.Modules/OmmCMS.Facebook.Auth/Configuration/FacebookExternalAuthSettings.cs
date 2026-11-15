using OmmCMS.Core.Configuration;

namespace OmmCMS.Facebook.Auth;

public class FacebookExternalAuthSettings : ISettings
{
    public string ClientKeyIdentifier { get; set; }
    public string ClientSecret { get; set; }
}