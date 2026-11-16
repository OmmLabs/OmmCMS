#nullable enable

using Microsoft.AspNetCore.Routing;
using OmmCMS.Core;
using OmmCMS.Core.Web;

namespace OmmCMS;

public static class WebDisplayHelperExtensions
{
    #region URL

    /// <inheritdoc cref="IWebHelper.ModifyQueryString(string?, string?, string?, string?)" />
    public static string ModifyQueryString(this IDisplayHelper displayHelper,
        string? url,
        string? queryModification,
        string? removeParamName = null,
        string? anchor = null)
    {
        return displayHelper.Resolve<IWebHelper>().ModifyQueryString(url, queryModification, removeParamName, anchor);
    }

    public static string? GenerateHelpUrl(this IDisplayHelper displayHelper, HelpTopic topic)
    {
        var seoCode = displayHelper.Resolve<IWorkContext>()?.WorkingLanguage?.UniqueSeoCode;
        if (seoCode.IsEmpty())
        {
            return topic?.EnPath;
        }

        return OmmCMSVersion.GenerateHelpUrl(seoCode, topic);
    }

    public static string GenerateHelpUrl(this IDisplayHelper displayHelper, string path)
    {
        var seoCode = displayHelper.Resolve<IWorkContext>()?.WorkingLanguage?.UniqueSeoCode;
        if (seoCode.IsEmpty())
        {
            return path;
        }

        return OmmCMSVersion.GenerateHelpUrl(seoCode, path);
    }

    #endregion

    #region Page Identity

    public static bool DisplayOmmCMSHint(this IDisplayHelper displayHelper)
        => !displayHelper.HttpContext.Items.ContainsKey(nameof(DisplayOmmCMSHint));

    public static bool IsMobileDevice(this IDisplayHelper displayHelper)
    {
        return displayHelper.HttpContext.GetItem(nameof(IsMobileDevice), () =>
        {
            var userAgent = displayHelper.Resolve<IUserAgent>();
            return userAgent.IsMobileDevice() && !userAgent.IsTablet();
        });
    }

    public static bool IsHomePage(this IDisplayHelper displayHelper)
    {
        return displayHelper.HttpContext.GetItem(nameof(IsHomePage), () =>
        {
            var routeValues = displayHelper.HttpContext.GetRouteData().Values;
            var response = displayHelper.HttpContext.Response;

            return response.StatusCode != 404 &&
                routeValues.GetControllerName().EqualsNoCase("home") &&
                routeValues.GetActionName().EqualsNoCase("index");
        });
    }

    public static string CurrentPageType(this IDisplayHelper displayHelper)
        => IdentifyPage(displayHelper).CurrentPageType;

    public static object CurrentPageId(this IDisplayHelper displayHelper)
        => IdentifyPage(displayHelper).CurrentPageId;

    public static int CurrentCategoryId(this IDisplayHelper displayHelper)
        => displayHelper.CurrentPageType() == "category" ? displayHelper.CurrentPageId().Convert<int>() : 0;

    public static int CurrentManufacturerId(this IDisplayHelper displayHelper)
        => displayHelper.CurrentPageType() == "manufacturer" ? displayHelper.CurrentPageId().Convert<int>() : 0;

    public static int CurrentProductId(this IDisplayHelper displayHelper)
        => displayHelper.CurrentPageType() == "product" ? displayHelper.CurrentPageId().Convert<int>() : 0;

    public static int CurrentTopicId(this IDisplayHelper displayHelper)
        => displayHelper.CurrentPageType() == "topic" ? displayHelper.CurrentPageId().Convert<int>() : 0;

    private static PageIdentity IdentifyPage(IDisplayHelper displayHelper)
    {
        return displayHelper.HttpContext.GetItem("PageIdentity", () =>
        {
            var context = displayHelper.HttpContext;
            var routeValues = context.GetRouteData().Values;
            var controllerName = routeValues.GetControllerName().ToLowerInvariant();
            var actionName = routeValues.GetActionName().ToLowerInvariant();

            string currentPageType = "system";
            object currentPageId = controllerName + "." + actionName;

            if (displayHelper.IsHomePage())
            {
                currentPageType = "home";
                currentPageId = 0;
            }
            else if (controllerName == "topic")
            {
                if (actionName == "topicdetails")
                {
                    currentPageType = "topic";
                    currentPageId = routeValues.Get("topicId")!;
                }
            }

            return new PageIdentity { CurrentPageId = currentPageId, CurrentPageType = currentPageType };
        });
    }

    record PageIdentity
    {
        public string CurrentPageType { get; set; } = default!;
        public object CurrentPageId { get; set; } = default!;
    }

    #endregion
}
