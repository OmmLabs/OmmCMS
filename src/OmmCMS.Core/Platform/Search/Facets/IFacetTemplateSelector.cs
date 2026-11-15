using OmmCMS.Core.Widgets;

namespace OmmCMS.Core.Search.Facets;

public interface IFacetTemplateSelector : IOrdered
{
    Widget GetTemplateWidget(FacetGroup facetGroup);
}