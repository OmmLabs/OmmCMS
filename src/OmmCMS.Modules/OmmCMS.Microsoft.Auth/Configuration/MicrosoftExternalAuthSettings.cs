using OmmCMS.Core.Configuration;

namespace OmmCMS.Microsoft.Auth;

public class MicrosoftExternalAuthSettings : ISettings
{
    public string ClientKeyIdentifier { get; set; }
    public string ClientSecret { get; set; }
}