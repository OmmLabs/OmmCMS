using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using OmmCMS.Core.Content.Topics;
using OmmCMS.Utilities;

namespace OmmCMS.Web.TagHelpers.Shared;

[HtmlTargetElement("collapsed-content")]
public class CollapsedContentTagHelper : TagHelper
{
    const string MaxHeightAttributeName = "sm-max-height";

    private readonly ContentSettings _contentSettings;

    public CollapsedContentTagHelper(ContentSettings contentSettings)
    {
        _contentSettings = contentSettings;
    }

    [HtmlAttributeName(MaxHeightAttributeName)]
    public int? MaxHeight { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (context.ShouldSuppressChildContent())
        {
            return;
        }

        output.TagName = null;
        await output.LoadAndSetChildContentAsync();

        if (_contentSettings.EnableHtmlTextCollapser && (MaxHeight == null || MaxHeight > 0))
        {
            var maxHeight = MaxHeight ?? _contentSettings.HtmlTextCollapsedHeight;
            var classes = output.Attributes.TryGetAttribute("class", out var attr) ? $"text-expander {attr.Value}" : "text-expander";
            var id = "text-expander" + CommonHelper.GenerateRandomInteger();

            var outer = new TagBuilder("div");
            outer.MergeAttribute("id", id);
            outer.Attributes.Add("class", classes);
            outer.Attributes.Add("data-max-height", maxHeight.ToString());

            if (output.Attributes.TryGetAttribute("style", out var style))
            {
                outer.Attributes.Add("style", style.Value.ToString());
            }

            var inner = new TagBuilder("div");
            inner.Attributes.Add("id", id + "-content");
            inner.Attributes.Add("class", "text-expander-content");

            output.WrapContentWith(outer, inner);
        }
    }
}
