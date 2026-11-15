global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Authentication.Twitter;
global using Microsoft.Extensions.Options;
global using OmmCMS.Web.Modelling;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Widgets;
using OmmCMS.Engine.Modularity;
using OmmCMS.Http;
using OmmCMS.Twitter.Auth.Components;

namespace OmmCMS.Twitter.Auth;

internal class Module : ModuleBase, IConfigurable, IExternalAuthenticationMethod
{
    public RouteInfo GetConfigurationRoute()
        => new("Configure", "TwitterAuth", new { area = "Admin" });

    public Widget GetDisplayWidget(int storeId)
        => new ComponentWidget(typeof(TwitterAuthViewComponent), null);

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