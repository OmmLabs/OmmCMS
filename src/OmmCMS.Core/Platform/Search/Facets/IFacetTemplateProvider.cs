using OmmCMS.Core.Widgets;

namespace OmmCMS.Core.Search.Facets;

public interface IFacetTemplateProvider
{
    Widget GetTemplateWidget(FacetGroup facetGroup);
}