global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Authentication.Facebook;
global using Microsoft.Extensions.Options;
global using OmmCMS.Web.Modelling;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Widgets;
using OmmCMS.Engine.Modularity;
using OmmCMS.Facebook.Auth.Components;
using OmmCMS.Http;

namespace OmmCMS.Facebook.Auth;

internal class Module : ModuleBase, IConfigurable, IExternalAuthenticationMethod
{
    public RouteInfo GetConfigurationRoute()
        => new("Configure", "FacebookAuth", new { area = "Admin" });

    public Widget GetDisplayWidget(int storeId)
        => new ComponentWidget(typeof(FacebookAuthViewComponent), null);

    public override async Task InstallAsync(ModuleInstallationContext context)
    {
        await ImportLanguageResourcesAsync();
        await base.InstallAsync(context);
    }

    public override async Task UninstallAsync()
    {
        await DeleteLanguageResourcesAsync();
        await base.UninstallAsync();
    }
}