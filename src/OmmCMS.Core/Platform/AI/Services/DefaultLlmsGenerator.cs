using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OmmCMS.Core.Content.Topics;
using OmmCMS.Core.Common.Configuration;
using OmmCMS.Core.Common.Services;
using OmmCMS.Core.Content.Menus;
using OmmCMS.Core.Data;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Seo;
using OmmCMS.Core.Stores;
using OmmCMS.Engine.Modularity;

namespace OmmCMS.Core.AI;

public class DefaultLlmsGenerator : ILlmsGenerator
{
    private readonly SmartDbContext _db;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;
    private readonly IAddressService _addressService;
    private readonly Lazy<IUrlHelper> _urlHelper;
    private readonly CompanyInformationSettings _companyInfoSettings;
    private readonly ContactDataSettings _contactSettings;
    private readonly HomePageSettings _homepageSettings;
    private readonly SocialSettings _socialSettings;

    protected IUrlHelper Url => _urlHelper.Value;

    public DefaultLlmsGenerator(
        SmartDbContext db,
        IStoreContext storeContext,
        IWorkContext workContext,
        IAddressService addressService,
        Lazy<IUrlHelper> urlHelper,
        CompanyInformationSettings companyInfoSettings,
        ContactDataSettings contactSettings,
        HomePageSettings homepageSettings,
        SocialSettings socialSettings)
    {
        _db = db;
        _storeContext = storeContext;
        _workContext = workContext;
        _addressService = addressService;
        _urlHelper = urlHelper;
        _companyInfoSettings = companyInfoSettings;
        _contactSettings = contactSettings;
        _homepageSettings = homepageSettings;
        _socialSettings = socialSettings;
    }

    public async Task GenerateLlms(TextWriter writer, HttpRequest httpRequest)
    {
        Guard.NotNull(writer);
        Guard.NotNull(httpRequest);

        await GenerateMetadata(writer, httpRequest);
        await GenerateContentPages(writer, httpRequest);
        await GenerateLinks(writer, httpRequest);
    }

    protected virtual async Task GenerateMetadata(TextWriter writer, HttpRequest httpRequest)
    {
        var company = _companyInfoSettings;
        var store = _storeContext.CurrentStore;
        var baseUrl = store.GetBaseUrl();

        writer.WriteLine($"# {store.Name} - LLM Directory");
        writer.WriteLine();

        var languages = await _db.Languages
            .AsNoTracking()
            .ApplyStandardFilter(false, store.Id)
            .ToListAsync();


        writer.WriteLine("## Metadata");

        WriteLine(writer, "Base URL", baseUrl);
        WriteLine(writer, "Title", _homepageSettings.MetaTitle);
        WriteLine(writer, "Description", _homepageSettings.MetaDescription);
        WriteLine(writer, "Operator", company.CompanyName);
        WriteLine(writer, "Legal Representatives", company.CompanyManagementDescription);

        // Address
        var formattedAddress = await _addressService.FormatAddressAsync(company);
        if (formattedAddress.HasValue())
        {
            var sep = formattedAddress.Contains(',') ? " | " : ", ";
            formattedAddress = formattedAddress.ReplaceLineEndings(sep);
            WriteLine(writer, "Address", formattedAddress);
        }

        WriteLine(writer, "Registered at", company.CommercialRegister);
        WriteLine(writer, "VAT ID", company.VatId);
        WriteLine(writer, "Support Email", _contactSettings.SupportEmailAddress);
        WriteLine(writer, "Support Phone", _contactSettings.HotlineTelephoneNumber);

        WriteLine(writer, "Currency", _workContext.WorkingCurrency?.CurrencyCode);
        WriteLine(writer, "Available Languages", string.Join(", ", languages.Select(x => x.UniqueSeoCode)));
        writer.WriteLine();
    }

    protected virtual async Task GenerateContentPages(TextWriter writer, HttpRequest httpRequest)
    {
        var store = _storeContext.CurrentStore;
        var baseUrl = new Uri(store.GetBaseUrl());
        var topics = await _db.Topics.AsNoTracking()
            .Where(x => !x.RenderAsWidget && !x.IsPasswordProtected && !x.SubjectToAcl && x.IncludeInSitemap)
            .ApplyStandardFilter(storeId: store.Id)
            .ToListAsync(httpRequest.HttpContext.RequestAborted);

        if (topics.Count == 0)
            return;

        writer.WriteLine("## Content pages");
        foreach (var topic in topics)
        {
            var link = await Url.TopicAsync(topic.SystemName);
            if (link.HasValue() && link != "#")
            {
                WriteLink(writer, topic.GetLocalized(x => x.Title), new Uri(baseUrl, link).ToString());
            }
        }
        writer.WriteLine();
    }

    protected virtual async Task GenerateLinks(TextWriter writer, HttpRequest httpRequest)
    {
        var store = _storeContext.CurrentStore;
        var baseUrl = store.GetBaseUrl();
        var scheme = httpRequest.Scheme;
        var social = _socialSettings;

        writer.WriteLine("## Discovery Links");
        WriteLink(writer, "Sitemap", baseUrl + "sitemap.xml");

        WriteLink(writer, "Contact & Support", Url.RouteUrl("ContactUs", null, scheme));
        WriteLink(writer, "Privacy Policy", new Uri(new Uri(baseUrl), await Url.TopicAsync("PrivacyInfo")).ToString());

        WriteLink(writer, "Facebook", social.FacebookLink);
        WriteLink(writer, "Twitter", social.TwitterLink);
        WriteLink(writer, "Instagram", social.InstagramLink);
        WriteLink(writer, "TikTok", social.TikTokLink);
        WriteLink(writer, "YouTube", social.YoutubeLink);
        WriteLink(writer, "Vimeo", social.VimeoLink);
        WriteLink(writer, "Pinterest", social.PinterestLink);
        WriteLink(writer, "Snapchat", social.SnapchatLink);
        WriteLink(writer, "Flickr", social.FlickrLink);
        WriteLink(writer, "LinkedIn", social.LinkedInLink);
        WriteLink(writer, "Xing", social.XingLink);
        WriteLink(writer, "Tumblr", social.TumblrLink);
        WriteLink(writer, "Ello", social.ElloLink);
        WriteLink(writer, "Behance", social.BehanceLink);
    }

    protected static void WriteLine(TextWriter writer, string prefix, string value)
    {
        if (value.HasValue())
        {
            writer.WriteLine($"- {prefix}: {value}");
        }
    }

    protected static void WriteLink(TextWriter writer, string name, string url)
    {
        if (url.HasValue() && url != "#")
        {
            writer.WriteLine($"- [{name}]({url})");
        }
    }
}
