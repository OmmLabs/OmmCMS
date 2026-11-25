using OmmCMS.Admin.Models.Common;
using OmmCMS.Core.Common.Services;
using OmmCMS.Core.Content.Topics;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Security;
using OmmCMS.Web.Models.DataGrid;

namespace OmmCMS.Admin.Controllers;

public class GenericAttributeController : AdminController
{
    private readonly SmartDbContext _db;
    private readonly IGenericAttributeService _genericAttributeService;

    public GenericAttributeController(SmartDbContext db, IGenericAttributeService genericAttributeService)
    {
        _db = db;
        _genericAttributeService = genericAttributeService;
    }

    [HttpPost]
    public IActionResult GenericAttributesList(GridCommand command, string entityName, int entityId)
    {
        var storeId = Services.StoreContext.CurrentStore.Id;
        ViewBag.StoreId = storeId;

        var totalCount = 0;
        var attributesItems = new List<GenericAttributeModel>();

        if (entityName.HasValue() && entityId > 0)
        {
            var (readPermission, _) = GetPermissionNames(entityName);

            if (readPermission.HasValue() && Services.Permissions.Authorize(readPermission))
            {
                var allAttributes = _genericAttributeService.GetAttributesForEntity(entityName, entityId).UnderlyingEntities;
                var attributes = allAttributes
                    .AsQueryable()
                    .Where(x => x.StoreId == storeId || x.StoreId == 0)
                    .ApplyGridCommand(command, true);

                totalCount = allAttributes.Count(x => x.StoreId == storeId || x.StoreId == 0);

                attributesItems = attributes
                    .Select(x => new GenericAttributeModel
                    {
                        Id = x.Id,
                        AttributeEntityId = x.EntityId,
                        EntityName = x.KeyGroup,
                        Key = x.Key,
                        Value = x.Value
                    })
                    .ToList();
            }
        }

        var gridModel = new GridModel<GenericAttributeModel>
        {
            Rows = attributesItems,
            Total = totalCount
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<IActionResult> GenericAttributeInsert(GenericAttributeModel model, string entityName, int entityId)
    {
        if (entityId <= 0 || GetPermissionNames(entityName).UpdatePermission.IsEmpty())
            return BadRequest();

        entityName = entityName.EqualsNoCase(nameof(Topic)) ? nameof(Topic) : nameof(Customer);
        if (!await Services.Permissions.AuthorizeAsync(GetPermissionNames(entityName).UpdatePermission))
            return Forbid();

        if (entityName == nameof(Topic) ? !await _db.Topics.AnyAsync(x => x.Id == entityId) : !await _db.Customers.AnyAsync(x => x.Id == entityId))
            return NotFound();

        model.Key = model.Key.TrimSafe();
        model.Value = model.Value.TrimSafe() ?? string.Empty;

        if (ModelState.IsValid)
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            var (readPermission, updatePermission) = GetPermissionNames(entityName);
            if (updatePermission.HasValue() && !Services.Permissions.Authorize(updatePermission))
            {
                NotifyError(await Services.Permissions.GetUnauthorizedMessageAsync(updatePermission));
                return Json(new { success = false });
            }
            else
            {
                var attr = await _db.GenericAttributes
                    .Where(x => x.EntityId == entityId && x.KeyGroup == entityName && x.Key == model.Key && x.StoreId == storeId)
                    .FirstOrDefaultAsync();

                if (attr == null)
                {
                    _db.GenericAttributes.Add(new GenericAttribute
                    {
                        StoreId = storeId,
                        KeyGroup = entityName,
                        EntityId = entityId,
                        Key = model.Key,
                        Value = model.Value
                    });
                    await _db.SaveChangesAsync();
                    return Json(new { success = true });
                }
                else
                {
                    NotifyWarning(T("Admin.Common.GenericAttributes.NameAlreadyExists", model.Key));
                    return Json(new { success = false });
                }
            }
        }
        else
        {
            var modelStateErrorMessages = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage);
            NotifyError(modelStateErrorMessages.FirstOrDefault());
            return Json(new { success = false });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GenericAttributeUpdate(GenericAttributeModel model)
    {
        var attr = await _db.GenericAttributes.FindByIdAsync(model.Id);
        if (attr == null)
            return NotFound();

        var entityName = attr.KeyGroup;
        var entityId = attr.EntityId;
        var currentStoreId = Services.StoreContext.CurrentStore.Id;
        if (!entityName.EqualsNoCase(model.EntityName) || entityId != model.AttributeEntityId || (attr.StoreId != 0 && attr.StoreId != currentStoreId))
            return BadRequest();

        if (GetPermissionNames(entityName).UpdatePermission.IsEmpty())
            return Forbid();

        model.Key = model.Key.TrimSafe();
        model.Value = model.Value.TrimSafe();

        if (ModelState.IsValid)
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            var (readPermission, updatePermission) = GetPermissionNames(entityName);

            if (updatePermission.IsEmpty() || entityId <= 0)
                return BadRequest();

            if (!await Services.Permissions.AuthorizeAsync(updatePermission))
            {
                NotifyError(await Services.Permissions.GetUnauthorizedMessageAsync(updatePermission));
                return Json(new { success = false });
            }
            else
            {
                // If the key changed, ensure it isn't being used by another attribute.
                if (!attr.Key.EqualsNoCase(model.Key))
                {
                    var attributes = _genericAttributeService.GetAttributesForEntity(entityName, entityId).UnderlyingEntities;
                    var attr2 = attributes
                        .Where(x => x.StoreId == attr.StoreId && x.Key.Equals(model.Key, StringComparison.InvariantCultureIgnoreCase))
                        .FirstOrDefault();

                    if (attr2 != null && attr2.Id != attr.Id)
                    {
                        NotifyWarning(T("Admin.Common.GenericAttributes.NameAlreadyExists", model.Key));
                        return Json(new { success = false });
                    }
                }

                attr.Key = model.Key;
                attr.Value = model.Value;

                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
        }
        else
        {
            var modelStateErrorMessages = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage);
            NotifyError(modelStateErrorMessages.FirstOrDefault());
            return Json(new { success = false });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GenericAttributeDelete(GridSelection selection, string entityName, int entityId)
    {
        var ids = selection.GetEntityIds();
        var numDeleted = 0;

        if (ids.Any())
        {
            var (readPermission, updatePermission) = GetPermissionNames(entityName);

            if (updatePermission.IsEmpty() || entityId <= 0)
                return BadRequest();

            if (!await Services.Permissions.AuthorizeAsync(updatePermission))
            {
                NotifyError(await Services.Permissions.GetUnauthorizedMessageAsync(updatePermission));
                return Json(new { Success = false, Count = numDeleted });
            }

            var storeId = Services.StoreContext.CurrentStore.Id;
            var canonicalName = entityName.EqualsNoCase(nameof(Topic)) ? nameof(Topic) : nameof(Customer);
            var scoped = _db.GenericAttributes.Where(x => ids.Contains(x.Id) && x.KeyGroup == canonicalName && x.EntityId == entityId && (x.StoreId == 0 || x.StoreId == storeId));
            if (await scoped.CountAsync() != ids.Distinct().Count())
                return BadRequest();

            numDeleted = await scoped.ExecuteDeleteAsync();
        }

        return Json(new { Success = true, Count = numDeleted });
    }

    private static (string ReadPermission, string UpdatePermission) GetPermissionNames(string entityName)
    {
        if (entityName.EqualsNoCase(nameof(Topic)))
        {
            return (Permissions.Cms.Topic.Read, Permissions.Cms.Topic.Update);
        }
        else if (entityName.EqualsNoCase(nameof(Customer)))
        {
            return (Permissions.Customer.Read, Permissions.Customer.Update);
        }

        return (null, null);
    }
}