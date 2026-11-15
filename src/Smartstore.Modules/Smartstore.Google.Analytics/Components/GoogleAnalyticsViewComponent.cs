using DouglasCrockford.JsMin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Identity;
using Smartstore.Google.Analytics.Services;
using Smartstore.Web.Components;

namespace Smartstore.Google.Analytics.Components;

public class GoogleAnalyticsViewComponent : SmartViewComponent
{
    private static readonly JsMinifier Minifier = new();

    private readonly GoogleAnalyticsSettings _settings;
    private readonly ICookieConsentManager _cookieConsentManager;
    private readonly GoogleAnalyticsScriptHelper _googleAnalyticsScriptHelper;

    public GoogleAnalyticsViewComponent(
        GoogleAnalyticsSettings settings,
        ICookieConsentManager cookieConsentManager,
        GoogleAnalyticsScriptHelper googleAnalyticsScriptHelper)
    {
        _settings = settings;
        _cookieConsentManager = cookieConsentManager;
        _googleAnalyticsScriptHelper = googleAnalyticsScriptHelper;
    }

    public async Task<IViewComponentResult> InvokeAsync(object model)
    {
        // If GoogleId is empty or is default don't render anything.
        if (!_settings.GoogleId.HasValue() || _settings.GoogleId == "UA-0000000-0")
        {
            return Empty();
        }

        var rootScript = string.Empty;

        try
        {
            // INFO: We must leave this here to handle Script settings which weren't updated yet. We can remove these parameters in the future.
            var cookiesAllowed = await _cookieConsentManager.IsCookieAllowedAsync(CookieType.Analytics);
            var adUserDataAllowed = await _cookieConsentManager.IsCookieAllowedAsync(CookieType.ConsentAdUserData);
            var adPersonalizationAllowed = await _cookieConsentManager.IsCookieAllowedAsync(CookieType.ConsentAdPersonalization);

            rootScript = _googleAnalyticsScriptHelper.GetTrackingScript(cookiesAllowed, adUserDataAllowed, adPersonalizationAllowed)
                .Replace("{ECOMMERCE}", string.Empty);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error creating scripts for Google Analytics tracking");
        }

        // If user has not accepted the cookie consent modify script tags to include data-consent attribute.
        var consented = !_settings.RenderWithUserConsentOnly || await _cookieConsentManager.IsCookieAllowedAsync(CookieType.Analytics);
        if (!consented)
        {
            rootScript = rootScript.Replace("<script>", "<script type=\"text/plain\">");
            rootScript = rootScript.Replace("<script", "<script data-consent=\"analytics\"");
            rootScript = rootScript.Replace("src=", "data-src=");
        }

        if (_settings.MinifyScripts && rootScript.HasValue())
        {
            rootScript = Minifier.Minify(rootScript);
        }

        var path = Url.Content("~/Modules/Smartstore.Google.Analytics/js/google-analytics.utils.js");
        //rootScript = $"<script {(consented ? string.Empty : "data-consent=\"analytics\" data-")}src='{path}'></script>\n{rootScript}";
        var scriptIncludeTag = _cookieConsentManager.GenerateScript(consented, CookieType.Analytics, path);
        rootScript = $"{scriptIncludeTag.ToHtmlString()}\n{rootScript}";

        return HtmlContent(rootScript);
    }
}