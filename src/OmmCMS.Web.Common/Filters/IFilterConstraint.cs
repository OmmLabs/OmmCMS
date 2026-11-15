using Microsoft.AspNetCore.Mvc.Filters;

namespace OmmCMS.Web.Filters;

public interface IFilterConstraint : IFilterFactory
{
    bool Match(ActionContext context);
}