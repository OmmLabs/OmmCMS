namespace OmmCMS.Web.Models.Common;

public partial class FooterModel : ModelBase
{
    public string StoreName { get; set; }
    public bool ShowThemeSelector { get; set; }
    public string NewsletterEmail { get; set; }
    public string OmmCMSHint { get; set; }
    public bool HideNewsletterBlock { get; set; }
    public bool ShowSocialLinks { get; set; }
    public List<SocialLink> SocialLinks { get; } = new();

    public void AddSocialLink(string href, string cssClass, string displayName)
    {
        SocialLinks.Add(new SocialLink { Href = href, CssClass = cssClass, DisplayName = displayName });
    }

    public class SocialLink
    {
        public string Href { get; set; }
        public string CssClass { get; set; }
        public string DisplayName { get; set; }
    }
}