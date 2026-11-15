global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Authentication.Google;
global using Microsoft.Extensions.Options;
global using OmmCMS.Web.Modelling;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Widgets;
using OmmCMS.Engine.Modularity;
using OmmCMS.Google.Auth.Components;
using OmmCMS.Http;

namespace OmmCMS.Google.Auth;

internal class Module : ModuleBase, IConfigurable, IExternalAuthenticationMethod
{
    public RouteInfo GetConfigurationRoute()
        => new("Configure", "GoogleAuth", new { area = "Admin" });

    public Widget GetDisplayWidget(int storeId)
        => new ComponentWidget(typeof(GoogleAuthViewComponent), null);

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