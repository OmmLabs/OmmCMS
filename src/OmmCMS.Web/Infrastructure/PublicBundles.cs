using OmmCMS.Web.Bundling;

namespace OmmCMS.Web.Infrastructure;

internal class PublicBundles : IBundleProvider
{
    public int Priority => 0;

    private static IEnumerable<string> ResolveThemeSourceFiles(DynamicBundleContext context)
    {
        var theme = context.RouteValues["theme"] as string;
        return [$"/Themes/{theme}/theme.scss"];
    }

    private static IEnumerable<string> ResolveRtlThemeSourceFiles(DynamicBundleContext context)
    {
        var theme = context.RouteValues["theme"] as string;
        return [$"/Themes/{theme}/theme-rtl.scss"];
    }

    private static bool IsValidTheme(DynamicBundleContext context)
    {
        var theme = context.RouteValues["theme"] as string;
        return context.ThemeRegistry.ContainsTheme(theme);
    }

    public void RegisterBundles(IApplicationContext appContext, IBundleCollection bundles)
    {
        if (!appContext.IsInstalled)
        {
            return;
        }

        var lib = "/lib/";
        var js = "/js/";


        /* Dynamic Sass themes --> /themes/[theme]/theme[-rtl].css
        -----------------------------------------------------*/
        bundles.Add(new DynamicStyleBundle("/themes/{theme}/theme.css")
            .WithConstraint(IsValidTheme)
            .Include(ResolveThemeSourceFiles));
        bundles.Add(new DynamicStyleBundle("/themes/{theme}/theme-rtl.css")
            .WithConstraint(IsValidTheme)
            .Include(ResolveRtlThemeSourceFiles));


        /* Public Common CSS --> /bundle/css/site-common.css
        -----------------------------------------------------*/
        bundles.Add(new StyleBundle("/bundle/css/site-common.css").Include(
            lib + "pnotify/css/pnotify.css",
            lib + "pnotify/css/pnotify.mobile.css",
            lib + "pnotify/css/pnotify.buttons.css"));


        /* Public Main --> /bundle/js/site.js
        -----------------------------------------------------*/
        bundles.Add(new ScriptBundle("/bundle/js/site.js").Include(
            // Vendors
            lib + "underscore/underscore.js",
            lib + "jquery/jquery.addeasing.js",
            lib + "jquery-ui/effect.js",
            lib + "jquery-ui/effect-shake.js",
            lib + "jquery/jquery.unobtrusive-ajax.js",
            lib + "jquery/jquery.validate.js",
            lib + "jquery/jquery.validate.unobtrusive.js",
            lib + "jquery/jquery.ba-outside-events.js",
            lib + "moment/moment.js",
            lib + "select2/js/select2.js",
            lib + "pnotify/js/pnotify.js",
            lib + "pnotify/js/pnotify.mobile.js",
            lib + "pnotify/js/pnotify.buttons.js",
            lib + "pnotify/js/pnotify.animate.js",
            lib + "slick/slick.js",
            lib + "aos/js/aos.js",
            lib + "popper/popper.js",
            lib + "bootstrap/js/bootstrap.js",
            // Common
            js + "underscore.mixins.js",
            js + "ommcms.system.js",
            js + "ommcms.touchevents.js",
            js + "ommcms.jquery.utils.js",
            js + "ommcms.globalization.js",
            js + "jquery.validate.unobtrusive.custom.js",
            js + "ommcms.viewport.js",
            js + "ommcms.scrollto.js",
            js + "ommcms.ajax.js",
            js + "ommcms.eventbroker.js",
            js + "ommcms.common.js",
            js + "ommcms.datetimepicker.js",
            js + "ommcms.globalinit.js",
            js + "ommcms.tabs.js",
            js + "ommcms.dialogs.js",
            js + "ommcms.selectwrapper.js",
            js + "ommcms.numberinput.js",
            js + "ommcms.throbber.js",
            js + "ommcms.thumbzoomer.js",
            js + "ommcms.articlelist.js",
            js + "ommcms.megamenu.js",
            js + "ommcms.offcanvas.js",
            js + "ommcms.parallax.js",
            js + "ommcms.media.js",
            js + "ommcms.cmp.js",
            js + "ommcms.accessibility.js",
            js + "ommcms.accessibility.plugins.js",
            js + "ommcms.accessibility.focustrap.js",
            // Shop
            js + "public.common.js",
            js + "public.search.js",
            js + "public.offcanvas-menu.js",
            js + "public.product.js"));
    }
}
