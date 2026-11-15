using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Autofac;

using OmmCMS.Core.Content.Media;
using OmmCMS.Core.Content.Menus;
using OmmCMS.Core.Content.Topics;
using OmmCMS.Core.Data;
using OmmCMS.Core.Localization;
using OmmCMS.Utilities;

namespace OmmCMS.Core.OutputCache;

public delegate Task<IEnumerable<string>> DisplayControlHandler(BaseEntity entity, SmartDbContext db, IComponentContext ctx);

public partial class DisplayControl : IDisplayControl
{
    private static readonly ConcurrentDictionary<Type, DisplayControlHandler> _handlers = new()
    {
        [typeof(Topic)] = (x, d, c) => ToTask("t" + x.Id),
        [typeof(MenuEntity)] = (x, d, c) => ToTask("mnu" + x.Id),
        [typeof(MenuItemEntity)] = (x, d, c) => ToTask("mnu" + ((MenuItemEntity)x).MenuId),
        [typeof(MediaFile)] = (x, d, c) => ToTask("mf" + x.Id),
        [typeof(LocalizedProperty)] = HandleLocalizedPropertyAsync
    };

    private readonly SmartDbContext _db;
    private readonly IComponentContext _componentContext;
    private readonly HashSet<BaseEntity> _entities = new();

    private bool _isIdle;
    private bool? _isUncacheableRequest;

    public DisplayControl(SmartDbContext db, IComponentContext componentContext)
    {
        _db = db;
        _componentContext = componentContext;
    }

    #region Static

    public static bool ContainsHandlerFor(Type type)
        => _handlers.ContainsKey(Guard.NotNull(type, nameof(type)));

    public static void RegisterHandlerFor(Type type, DisplayControlHandler handler)
    {
        Guard.NotNull(type, nameof(type));
        Guard.NotNull(handler, nameof(handler));

        _handlers.TryAdd(type, handler);
    }

    #endregion

    #region Handlers

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Task<IEnumerable<string>> ToTask(params string[] tags)
    {
        return Task.FromResult<IEnumerable<string>>(tags);
    }

    private static Task<IEnumerable<string>> HandleLocalizedPropertyAsync(BaseEntity entity, SmartDbContext db, IComponentContext ctx)
    {
        var lp = (LocalizedProperty)entity;
        string prefix = null;

        switch (lp.LocaleKeyGroup)
        {
            case nameof(Topic):
                prefix = "t";
                break;
            case nameof(MediaFile):
                prefix = "mf";
                break;
        }

        return prefix.HasValue() ? ToTask(prefix + lp.EntityId) : Task.FromResult(Enumerable.Empty<string>());
    }

    #endregion

    public IDisposable BeginIdleScope()
    {
        _isIdle = true;
        return new ActionDisposable(() => _isIdle = false);
    }

    public virtual void Announce(BaseEntity entity)
    {
        if (!_isIdle && entity != null)
        {
            _entities.Add(entity);
        }
    }

    public bool IsDisplayed(BaseEntity entity)
    {
        if (entity == null)
            return false;

        return _entities.Contains(entity);
    }

    public void MarkRequestAsUncacheable()
    {
        // First wins: subsequent calls should not be able to cancel this
        if (!_isIdle)
            _isUncacheableRequest = true;
    }

    public bool IsUncacheableRequest
        => _isUncacheableRequest.GetValueOrDefault() == true;

    public virtual Task<IEnumerable<string>> GetCacheControlTagsForAsync(BaseEntity entity)
    {
        var empty = Enumerable.Empty<string>();

        if (entity == null || entity.IsTransientRecord())
        {
            return Task.FromResult(empty);
        }

        if (!_handlers.TryGetValue(entity.GetType(), out var handler))
        {
            return Task.FromResult(empty);
        }

        return handler.Invoke(entity, _db, _componentContext);
    }

    public async Task<string[]> GetAllCacheControlTagsAsync()
    {
        var allTags = new HashSet<string>();

        foreach (var entity in _entities)
        {
            if (entity.Id > 0)
            {
                var entityTags = await GetCacheControlTagsForAsync(entity);
                if (entityTags != null)
                {
                    foreach (var tag in entityTags)
                    {
                        allTags.Add(tag);
                    }
                }
            }
        }

        return allTags.ToArray();
    }
}
