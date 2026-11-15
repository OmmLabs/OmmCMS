using OmmCMS.Core.Data;
using OmmCMS.Core.Stores;
using OmmCMS.Data.Hooks;
using OmmCMS.Events;

namespace OmmCMS.Core.Common.Services;

public partial class GenericAttributeService : AsyncDbSaveHook<GenericAttribute>, IGenericAttributeService
{
    private readonly SmartDbContext _db;
    private readonly IStoreContext _storeContext;
    private readonly IEventPublisher _eventPublisher;

    // Key = (EntityName, EntityId)
    private readonly Dictionary<(string, int), GenericAttributeCollection> _collectionCache = [];

    public GenericAttributeService(SmartDbContext db, IStoreContext storeContext, IEventPublisher eventPublisher)
    {
        _db = db;
        _storeContext = storeContext;
        _eventPublisher = eventPublisher;
    }

    #region Hook

    public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        => Task.FromResult(HookResult.Ok);

    #endregion

    public virtual GenericAttributeCollection GetAttributesForEntity(string entityName, int entityId)
    {
        Guard.NotEmpty(entityName);

        if (entityId <= 0)
        {
            // Return a read-only collection
            return new GenericAttributeCollection(entityName);
        }

        var key = (entityName.ToLowerInvariant(), entityId);

        if (!_collectionCache.TryGetValue(key, out var collection))
        {
            var query = from attr in _db.GenericAttributes
                        where attr.EntityId == entityId && attr.KeyGroup == entityName
                        select attr;

            collection = new GenericAttributeCollection(query, entityName, entityId, _storeContext.CurrentStore.Id);
            _collectionCache[key] = collection;
        }

        return collection;
    }

    public virtual async Task PrefetchAttributesAsync(string entityName, int[] entityIds)
    {
        Guard.NotEmpty(entityName);
        Guard.NotNull(entityIds);

        if (entityIds.Length == 0)
        {
            return;
        }

        // Reduce entityIds by already loaded collections.
        var ids = new List<int>(entityIds.Length);
        foreach (var id in entityIds.Distinct().OrderBy(x => x))
        {
            if (!_collectionCache.ContainsKey((entityName.ToLowerInvariant(), id)))
            {
                ids.Add(id);
            }
        }

        var storeId = _storeContext.CurrentStore.Id;

        var attributes = await _db.GenericAttributes
            .Where(x => ids.Contains(x.EntityId) && x.KeyGroup == entityName)
            .ToListAsync();

        var groupedAttributes = attributes
            .GroupBy(x => x.EntityId)
            .ToList();

        foreach (var group in groupedAttributes)
        {
            var entityId = group.Key;
            var collection = new GenericAttributeCollection(
                _db.GenericAttributes.Where(x => x.EntityId == entityId && x.KeyGroup == entityName),
                entityName,
                entityId,
                storeId,
                group.ToList());

            _collectionCache[(entityName.ToLowerInvariant(), entityId)] = collection;
        }
    }
}