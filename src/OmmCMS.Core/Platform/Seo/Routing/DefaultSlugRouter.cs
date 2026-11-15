using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace OmmCMS.Core.Seo.Routing;

public class DefaultSlugRouter : SlugRouter
{
    public override RouteValueDictionary GetRouteValues(UrlRecord urlRecord, RouteValueDictionary values, RouteTarget routeTarget = RouteTarget.PublicView)
    {
        if (routeTarget == RouteTarget.Edit)
        {
            return null;
        }

        switch (urlRecord.EntityName.ToLowerInvariant())
        {
            case "topic":
                return new RouteValueDictionary
                {
                    { "area", string.Empty },
                    { "controller", "Topic" },
                    { "action", "TopicDetails" },
                    { "topicId", urlRecord.EntityId },
                    { "entity", urlRecord }
                };
        }

        return null;
    }

    public override IEndpointConventionBuilder MapRoutes(IEndpointRouteBuilder routes)
    {
        return routes.MapComposite(
        [
            routes.MapLocalizedControllerRoute("Topic", UrlPatternFor("Topic"), new { controller = "Topic", action = "TopicDetails" })
        ]);
    }
}
