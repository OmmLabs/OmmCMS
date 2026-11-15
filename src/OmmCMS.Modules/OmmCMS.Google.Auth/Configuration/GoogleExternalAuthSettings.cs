using OmmCMS.Core.Configuration;

namespace OmmCMS.Google.Auth;

public class GoogleExternalAuthSettings : ISettings
{
    public string ConsumerKey { get; set; }
    public string ConsumerSecret { get; set; }
}