using OmmCMS.Core.Common.Configuration;
using OmmCMS.Core.Content.Menus;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Theming;
using OmmCMS.Utilities;
using OmmCMS.Web.Models.Common;

namespace OmmCMS.Web.Components;

public class FooterViewComponent : SmartViewComponent
{

    private readonly IThemeRegistry _themeRegistry;
    private readonly IWidgetProvider _widgetProvider;
    private readonly IDisplayHelper _displayHelper;
    private readonly ThemeSettings _themeSettings;
    private readonly CustomerSettings _customerSettings;
    private readonly SocialSettings _socialSettings;
    private readonly PrivacySettings _privacySettings;

    public FooterViewComponent(
        IThemeRegistry themeRegistry,
        IWidgetProvider widgetProvider,
        IDisplayHelper displayHelper,
        ThemeSettings themeSettings,
        CustomerSettings customerSettings,
        SocialSettings socialSettings,
        PrivacySettings privacySettings)
    {
        _themeRegistry = themeRegistry;
        _widgetProvider = widgetProvider;
        _displayHelper = displayHelper;
        _themeSettings = themeSettings;
        _customerSettings = customerSettings;
        _socialSettings = socialSettings;
        _privacySettings = privacySettings;
    }

    public IViewComponentResult Invoke()
    {
        var store = Services.StoreContext.CurrentStore;
        var availableStoreThemes = !_themeSettings.AllowCustomerToSelectTheme
            ? []
            : _themeRegistry.GetThemeDescriptors()
                .Select(x =>
                {
                    return new StoreThemeModel
                    {
                        Name = x.Name,
                        Title = x.FriendlyName
                    };
                })
                .ToList();

        var model = new FooterModel
        {
            StoreName = store.Name,
            ShowThemeSelector = availableStoreThemes.Count > 1,
            HideNewsletterBlock = _customerSettings.HideNewsletterBlock,
            ShowSocialLinks = _socialSettings.ShowSocialLinksInFooter
        };

        if (model.ShowSocialLinks)
        {
            TryAddSocialLink(_socialSettings.FacebookLink, "facebook-f", "Facebook");
            TryAddSocialLink(_socialSettings.TwitterLink, "x-twitter", "X (Twitter)");
            TryAddSocialLink(_socialSettings.InstagramLink, "instagram", "Instagram");
            TryAddSocialLink(_socialSettings.TikTokLink, "tiktok", "TikTok");
            TryAddSocialLink(_socialSettings.YoutubeLink, "youtube", "Youtube");
            TryAddSocialLink(_socialSettings.VimeoLink, "vimeo", "Vimeo");
            TryAddSocialLink(_socialSettings.PinterestLink, "pinterest-p", "Pinterest");
            TryAddSocialLink(_socialSettings.SnapchatLink, "snapchat", "Snapchat");
            TryAddSocialLink(_socialSettings.FlickrLink, "flickr", "Flickr");
            TryAddSocialLink(_socialSettings.LinkedInLink, "linkedin", "LinkedIn");
            TryAddSocialLink(_socialSettings.XingLink, "xing", "Xing");
            TryAddSocialLink(_socialSettings.TumblrLink, "tumblr", "Tumblr");
            TryAddSocialLink(_socialSettings.ElloLink, "ello", "Ello");
            TryAddSocialLink(_socialSettings.BehanceLink, "behance", "Behance");
        }

        if (_displayHelper.DisplayOmmCMSHint())
        {
            model.OmmCMSHint = $"<a href='https://ommlabs.ommrudraksha.com' class='sm-hint' target='_blank'><strong>OmmCMS</strong></a> by OmmLabs &copy; {DateTime.Now.Year}";
        }

        if (ShouldRenderGDPR())
        {
            _widgetProvider.RegisterViewComponent<GdprConsentViewComponent>("gdpr_consent_small", new { isSmall = true });
            HttpContext.Items["GdprConsentRendered"] = true;
        }

        return View(model);

        void TryAddSocialLink(string href, string cssClass, string displayName)
        {
            if (href.HasValue())
            {
                model.AddSocialLink(href, cssClass, displayName);
            }
        }
    }

    private bool ShouldRenderGDPR()
    {
        if (!_privacySettings.DisplayGdprConsentOnForms)
            return false;

        if (HttpContext.Items.Keys.Contains("GdprConsentRendered"))
            return false;

        return true;
    }
}
