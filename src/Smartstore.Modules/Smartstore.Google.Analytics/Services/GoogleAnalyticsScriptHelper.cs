using System.Text.RegularExpressions;
using Smartstore.Core;

namespace Smartstore.Google.Analytics.Services;

/// <summary>
/// Helper class to prepare consent-aware site tracking scripts for Google Analytics (GA).
/// </summary>
public class GoogleAnalyticsScriptHelper
{
    private readonly static Regex _rgScript = new(@"{(?<token>([A-Z]+))}", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private readonly GoogleAnalyticsSettings _settings;
    private readonly IWorkContext _workContext;

    public GoogleAnalyticsScriptHelper(GoogleAnalyticsSettings settings, IWorkContext workContext)
    {
        _settings = settings;
        _workContext = workContext;
    }

    public Localizer T { get; set; } = NullLocalizer.Instance;

    /// <summary>
    /// Generates global GA script
    /// </summary>
    /// <param name="cookiesAllowed">Defines whether cookies can be used by Google and sets ad_storage & analytics_storage of the consent tag accordingly.</param>
    public string GetTrackingScript(bool cookiesAllowed, bool adUserDataAllowed, bool adPersonalizationAllowed)
    {
        using var writer = new StringWriter();

        var globalTokens = new Dictionary<string, Func<string>>
        {
            ["GOOGLEID"] = () => _settings.GoogleId,
            ["OPTOUTCOOKIE"] = GetOptOutCookieScript,

            // INFO: We must leave this here to handle Script settings which weren't updated yet. We can remove this in the future.
            // If no consent to third party cookies was given, set storage type to denied.
            ["STORAGETYPE"] = () => cookiesAllowed ? "granted" : "denied",
            ["USERID"] = _workContext.CurrentCustomer.CustomerGuid.ToString,
            ["ADUSERDATA"] = () => adUserDataAllowed ? "granted" : "denied",
            ["ADPERSONALIZATION"] = () => adPersonalizationAllowed ? "granted" : "denied"
        };

        ParseScript(_settings.TrackingScript, writer, globalTokens);

        return writer.ToString();
    }

    /// <summary>
    /// Builds script to provide functions for cookie usage opt-out. Usage is described in AdminInstruction.
    /// </summary>
    private string GetOptOutCookieScript()
    {
        return @$"
				var gaProperty = '{_settings.GoogleId}'; 
				var disableStr = 'ga-disable-' + gaProperty; 
				if (document.cookie.indexOf(disableStr + '=true') > -1) {{ 
					window[disableStr] = true;
				}} 
				function gaOptout() {{ 
					document.cookie = disableStr + '=true; expires=Thu, 31 Dec 2099 23:59:59 UTC; path=/'; 
					window[disableStr] = true; 
					alert({T("Plugins.Widgets.GoogleAnalytics.OptOutNotification").JsValue});
                }}";
    }

    private static void ParseScript(string input, TextWriter writer, IDictionary<string, Func<string>> tokens)
    {
        var replacedScript = _rgScript.Replace(input, match =>
        {
            var token = match.Groups[1].Value;

            if (tokens.TryGetValue(token, out var func))
            {
                return func();
            }

            // If no func is found we return token in order to not remove any tokens which shouldn't replaced explicitly.
            return match.Value;
        });

        writer.Write(replacedScript);
    }
}
