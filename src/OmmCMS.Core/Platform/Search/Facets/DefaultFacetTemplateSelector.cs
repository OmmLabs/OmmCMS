using OmmCMS.Core.Widgets;

namespace OmmCMS.Core.Search.Facets;

public class DefaultFacetTemplateSelector : IFacetTemplateSelector
{
    public int Ordinal => -100;

    public Widget GetTemplateWidget(FacetGroup facetGroup)
    {
        var templateName = GetTemplateName(facetGroup);
        if (templateName.IsEmpty())
        {
            return null;
        }

        var widget = new ComponentWidget("FacetGroup", new { facetGroup, templateName })
        {
            Order = facetGroup.DisplayOrder
        };

        return widget;
    }

    private static string GetTemplateName(FacetGroup group)
    {
        switch (group?.Kind)
        {
            case FacetGroupKind.Category:
            case FacetGroupKind.Forum:
            case FacetGroupKind.Customer:
            case FacetGroupKind.Date:
                return group.IsMultiSelect ? "MultiSelect" : "SingleSelect";
            case FacetGroupKind.Unknown:
            case FacetGroupKind.Attribute:
            default:
                return null;
        }
    }
}
