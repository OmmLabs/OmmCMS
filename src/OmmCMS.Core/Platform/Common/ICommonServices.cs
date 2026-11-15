using Autofac;
using OmmCMS.Caching;
using OmmCMS.Core.Common.Services;
using OmmCMS.Core.Configuration;
using OmmCMS.Core.Content.Media;
using OmmCMS.Core.Data;
using OmmCMS.Core.Localization;
using OmmCMS.Core.Logging;
using OmmCMS.Core.OutputCache;
using OmmCMS.Core.Security;
using OmmCMS.Core.Stores;
using OmmCMS.Core.Web;
using OmmCMS.Diagnostics;
using OmmCMS.Events;

namespace OmmCMS.Core;

public interface ICommonServices
{
    IComponentContext Container { get; }
    IApplicationContext ApplicationContext { get; }
    ICacheFactory CacheFactory { get; }
    ICacheManager Cache { get; }
    IRequestCache RequestCache { get; }
    SmartDbContext DbContext { get; }
    IStoreContext StoreContext { get; }
    IWorkContext WorkContext { get; }
    IWebHelper WebHelper { get; }
    IEventPublisher EventPublisher { get; }
    ILocalizationService Localization { get; }
    ISettingService Settings { get; }
    ISettingFactory SettingFactory { get; }
    ILoggerFactory LoggerFactory { get; }
    IActivityLogger ActivityLogger { get; }
    INotifier Notifier { get; }
    IPermissionService Permissions { get; }
    IChronometer Chronometer { get; }
    IDateTimeHelper DateTimeHelper { get; }
    IMediaService MediaService { get; }
    IDisplayControl DisplayControl { get; }
    ICurrencyService CurrencyService { get; }
    IStoreMappingService StoreMappingService { get; }
}

public static class ICommonServicesExtensions
{
    public static TService Resolve<TService>(this ICommonServices services)
        => services.Container.Resolve<TService>();

    public static TService ResolveKeyed<TService>(this ICommonServices services, object serviceKey)
        => services.Container.ResolveKeyed<TService>(serviceKey);

    public static TService ResolveNamed<TService>(this ICommonServices services, string serviceName)
        => services.Container.ResolveNamed<TService>(serviceName);
}