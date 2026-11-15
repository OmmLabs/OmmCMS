using OmmCMS.Core.Configuration;

namespace OmmCMS.Twitter.Auth;

public class TwitterExternalAuthSettings : ISettings
{
    public string ConsumerKey { get; set; }
    public string ConsumerSecret { get; set; }
}