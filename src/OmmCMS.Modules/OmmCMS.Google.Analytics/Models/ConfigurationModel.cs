namespace OmmCMS.Google.Analytics.Models;

[LocalizedDisplay("Plugins.Widgets.GoogleAnalytics.")]
public class ConfigurationModel : ModelBase
{
    [LocalizedDisplay("*GoogleId")]
    public string GoogleId { get; set; }

    [LocalizedDisplay("*RenderWithUserConsentOnly")]
    public bool RenderWithUserConsentOnly { get; set; }

    [LocalizedDisplay("*DisplayCookieInfosForAds")]
    public bool DisplayCookieInfosForAds { get; set; }

    [LocalizedDisplay("*TrackingScript")]
    [UIHint("Textarea"), AdditionalMetadata("rows", 12)]
    public string TrackingScript { get; set; }

    [LocalizedDisplay("*MinifyScripts")]
    public bool MinifyScripts { get; set; }

    public bool ScriptUpdateRecommended { get; set; }
}