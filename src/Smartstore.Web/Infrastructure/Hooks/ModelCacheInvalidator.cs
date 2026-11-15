using Smartstore.Caching;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Data.Hooks;
using Smartstore.Utilities;

namespace Smartstore.Web.Infrastructure.Hooks;

internal partial class ModelCacheInvalidator : IDbSaveHook
{
    #region Consts

    /// <summary>
    /// Key for TopicModel caching
    /// </summary>
    /// <remarks>
    /// {0} : topic id/systemname
    /// {1} : language id
    /// {2} : store id
    /// {3} : role ids
    /// </remarks>
    public const string TOPIC_BY_SYSTEMNAME_KEY = "pres:topic:page.bysystemname-{0}-{1}-{2}-{3}";
    public const string TOPIC_BY_ID_KEY = "pres:topic:page.byid-{0}-{1}-{2}-{3}";
    public const string TOPIC_PATTERN_KEY = "pres:topic:page*";

    /// <summary>
    /// Key for states by country id
    /// </summary>
    /// <remarks>
    /// {0} : country ID
    /// {1} : addEmptyStateIfRequired value
    /// {2} : language ID
    /// </remarks>
    public const string STATEPROVINCES_BY_COUNTRY_MODEL_KEY = "pres:stateprovinces:bycountry-{0}-{1}-{2}";
    public const string STATEPROVINCES_PATTERN_KEY = "pres:stateprovinces:*";

    /// <summary>
    /// Key for available languages
    /// </summary>
    /// <remarks>
    /// {0} : language ID
    /// {1} : current store ID
    /// {2} : LocalizationSettings.UseNativeNameInLanguageSelector
    /// </remarks>
    public const string AVAILABLE_LANGUAGES_MODEL_KEY = "pres:languages:all-{0}-{1}-{2}";
    public const string AVAILABLE_LANGUAGES_PATTERN_KEY = "pres:languages:*";

    /// <summary>
    /// Key for favicon
    /// </summary>
    /// <remarks>
    /// {0} : current store ID
    /// </remarks>
    public const string STORE_FAVICON_MODEL_KEY = "pres:favicon-{0}";
    public const string STORE_FAVICON_PATTERN_KEY = "pres:favicon*";

    /// <summary>
    /// Key for shop logo
    /// </summary>
    /// <remarks>
    /// {0} : current store ID, {1} = Url scheme, {2} = Host
    /// </remarks>
    public const string STORE_LOGO_MODEL_KEY = "pres:logo-{0}-{1}-{2}";
    public const string STORE_LOGO_PATTERN_KEY = "pres:logo*";

    #endregion

    private readonly ICacheManager _cache;

    public ModelCacheInvalidator(ICacheManager cache)
    {
        _cache = cache;
    }

    public Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        => Task.FromResult(HookResult.Void);

    public async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
    {
        var state = entry.InitialState;
        var entity = entry.Entity;
        var result = HookResult.Ok;

        if (entity is Topic)
        {
            if (state != EntityState.Added)
            {
                await _cache.RemoveByPatternAsync(TOPIC_PATTERN_KEY);
            }
        }
        else if (entity is Language)
        {
            // Clear all localizable models
            await _cache.RemoveByPatternAsync(TOPIC_PATTERN_KEY);
            await _cache.RemoveByPatternAsync(STATEPROVINCES_PATTERN_KEY);
            await _cache.RemoveByPatternAsync(AVAILABLE_LANGUAGES_PATTERN_KEY);
        }
        else if (entity is Store)
        {
            await _cache.RemoveByPatternAsync(STORE_FAVICON_PATTERN_KEY);
            await _cache.RemoveByPatternAsync(STORE_LOGO_PATTERN_KEY);
        }
        else if (entity is StateProvince)
        {
            await _cache.RemoveByPatternAsync(STATEPROVINCES_PATTERN_KEY);
        }
        else if (entity is LocalizedProperty lp)
        {
            if (lp.LocaleKeyGroup == nameof(Topic))
            {
                if (state != EntityState.Added)
                {
                    await _cache.RemoveByPatternAsync(TOPIC_PATTERN_KEY);
                }
            }
        }
        else
        {
            // Register as void hook for all other entity type/state combis
            result = HookResult.Void;
        }

        return result;
    }

    public Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        => Task.CompletedTask;

    public Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        => Task.CompletedTask;
}