using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using OmmCMS.Core.Localization;

namespace OmmCMS.Web.Rendering;

public static class VueSlotTemplateExtensions
{
    /// <summary>
    /// Renders a labeled currency name for grids including a link to the edit page. Not intended to be used outside of grids.
    /// </summary>
    /// <param name="helper"></param>
    /// <param name="isPrimaryCurrencyExpression">Row expression of IsPrimaryCurrencyExpression property.</param>
    /// <param name="isPrimaryExchangeCurrencyExpression">Row expression of IsPrimaryExchangeCurrencyExpression property.</param>
    /// <param name="urlExpression">Row expression of the edit page URL.</param>
    /// <param name="valueExpression">Value expression of the name cell.</param>
    /// <param name="linkTarget">Value of the HTML target attribute.</param>
    /// <returns>Labeled currency name.</returns>
    public static IHtmlContent LabeledCurrencyName(
        this IHtmlHelper helper,
        string isPrimaryCurrencyExpression = "item.row.IsPrimaryCurrency",
        string isPrimaryExchangeCurrencyExpression = "item.row.IsPrimaryExchangeCurrency",
        string urlExpression = "item.row.EditUrl",
        string valueExpression = "item.value",
        string linkTarget = null)
    {
        var builder = new SmartHtmlContentBuilder();
        var localizationService = helper.ViewContext.HttpContext.RequestServices.GetService<ILocalizationService>();

        if (isPrimaryCurrencyExpression.HasValue())
        {
            var label = new TagBuilder("span");
            label.Attributes.Add("v-if", isPrimaryCurrencyExpression);
            label.Attributes.Add("class", "badge badge-subtle badge-ring badge-warning mr-1");
            label.InnerHtml.Append(localizationService.GetResource("Admin.Configuration.Currencies.Fields.IsPrimaryStoreCurrency"));

            builder.AppendHtml(label);
        }

        if (isPrimaryExchangeCurrencyExpression.HasValue())
        {
            var label = new TagBuilder("span");
            label.Attributes.Add("v-if", isPrimaryExchangeCurrencyExpression);
            label.Attributes.Add("class", "badge badge-subtle badge-ring badge-info mr-1");
            label.InnerHtml.Append(localizationService.GetResource("Admin.Configuration.Currencies.Fields.IsPrimaryExchangeRateCurrency"));

            builder.AppendHtml(label);
        }

        var isLink = urlExpression.HasValue();
        var name = new TagBuilder(isLink ? "a" : "span");
        name.Attributes.Add("class", "text-truncate");
        name.InnerHtml.AppendHtml("{{ " + valueExpression + " }}");

        if (isLink)
        {
            name.Attributes.Add("v-bind:href", urlExpression);
            if (linkTarget.HasValue())
            {
                name.Attributes.Add("target", linkTarget);
            }
        }

        builder.AppendHtml(name);

        return builder;
    }

    /// <summary>
    /// Renders a labeled variant attribute value name including link icon and color squares for grids.
    /// Not intended to be used outside of grids.
    /// </summary>
    /// <returns>Labeled variant attribute value name</returns>
    public static IHtmlContent VariantAttributeValueName(this IHtmlHelper _)
    {
        var builder = new SmartHtmlContentBuilder();
        builder.AppendHtml("<sm-icon v-if='item.row.TypeNameIcon' :name='item.row.TypeNameIcon' class='mr-2' :title='item.row.TypeName'></sm-icon>");
        builder.AppendHtml(_.ColorPalette());
        builder.AppendHtml("<span><a href='javascript:;' class='edit-variant-attribute-value' :data-key='item.row.Id'>{{ item.value }} {{ item.row.QuantityInfo }}</a></span>");

        return builder;
    }

    /// <summary>
    /// Renders the primary and additional colors of an attribute value for grids.
    /// Not intended to be used outside of grids.
    /// </summary>
    /// <returns>Attribute value color palette.</returns>
    public static IHtmlContent ColorPalette(this IHtmlHelper _)
    {
        var swatch = new TagBuilder("span");
        swatch.Attributes.Add("class", "color");
        swatch.Attributes.Add(":class", "{ 'swatch-multicolor': item.row.Colors.SwatchColors.length > 1 }");
        swatch.Attributes.Add(":style", "item.row.Colors.SwatchStyle");
        swatch.Attributes.Add(":title", "item.row.Colors.SwatchColors.join(', ')");

        var colorSpan = new TagBuilder("span");
        colorSpan.Attributes.Add("v-if", "item.row.Colors?.SwatchColors.length");
        colorSpan.Attributes.Add("class", "color-container");
        colorSpan.InnerHtml.AppendHtml(swatch);

        return colorSpan;
    }

    /// <summary>
    /// Renders a specification attribute option name (including color square) for grids. Not intended to be used outside of grids.
    /// </summary>
    /// <returns>Specification attribute option name.</returns>
    public static IHtmlContent SpecificationAttributeOptionName(this IHtmlHelper _)
    {
        var colorSpan = new TagBuilder("span");
        colorSpan.Attributes.Add("v-if", "item.row.Color?.length > 0");
        colorSpan.Attributes.Add("class", "color-container");
        colorSpan.InnerHtml.AppendHtml("<span class='color' :style='{ background: item.row.Color }' :title='item.row.Color'>&nbsp;</span>");

        var builder = new SmartHtmlContentBuilder();
        builder.AppendHtml(colorSpan);
        builder.AppendHtml("<a href='javascript:;' class='edit-specification-attribute-option' :data-id='item.row.Id'>{{ item.value }}</a>");

        return builder;
    }
}
