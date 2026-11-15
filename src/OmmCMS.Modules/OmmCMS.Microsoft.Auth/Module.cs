global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
global using Microsoft.Extensions.Options;
global using OmmCMS.Web.Modelling;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Widgets;
using OmmCMS.Engine.Modularity;
using OmmCMS.Http;
using OmmCMS.Microsoft.Auth.Components;

namespace OmmCMS.Microsoft.Auth;

internal class Module : ModuleBase, IConfigurable, IExternalAuthenticationMethod
{
    public RouteInfo GetConfigurationRoute()
        => new("Configure", "MicrosoftAuth", new { area = "Admin" });

    public Widget GetDisplayWidget(int storeId)
        => new ComponentWidget(typeof(MicrosoftAuthViewComponent), null);

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