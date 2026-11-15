using Microsoft.AspNetCore.Builder;
using OmmCMS.Engine.Initialization;

namespace OmmCMS.Bootstrapping;

public static class ApplicationInitializerExtensions
{
    /// <summary>
    /// Executes <see cref="IApplicationInitializer"/> implementations during the very first request.
    /// </summary>
    public static IApplicationBuilder UseApplicationInitializer(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApplicationInitializerMiddleware>();
    }
}