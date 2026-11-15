using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Data;
using Smartstore.Web.Rendering;
using Smartstore.Web.Infrastructure;

namespace Smartstore.Web.Controllers;

public partial class EntityController : PublicController
{
    private readonly SmartDbContext _db;
    private readonly CustomerSettings _customerSettings;
    private readonly Lazy<ILocalizedEntityService> _locEntityService;

    public EntityController(
        SmartDbContext db,
        CustomerSettings customerSettings,
        Lazy<ILocalizedEntityService> locEntityService)
    {
        _db = db;
        _customerSettings = customerSettings;
        _locEntityService = locEntityService;
    }

    public async Task<IActionResult> Picker(EntityPickerModel model)
    {
        if (OmmCmsCommerceIsolation.IsCommercePickerEntity(model.EntityType))
        {
            return NotFound();
        }

        if (model.EntityType.EqualsNoCase("customer"))
        {
            if (!await CanAccessCustomerPicker())
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            ViewBag.AvailableCustomerSearchTypes = new List<SelectListItem>
            {
                new() { Text = "Name", Value = "Name", Selected = true },
                new() { Text = "Email", Value = "Email" }
            };

            if (_customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled)
            {
                ViewBag.AvailableCustomerSearchTypes.Add(new SelectListItem { Text = T("Account.Fields.CustomerNumber"), Value = "CustomerNumber" });
            }
        }

        return PartialView(model);
    }

    [HttpPost]
    [ActionName("Picker")]
    public async Task<IActionResult> PickerPost(EntityPickerModel model)
    {
        if (OmmCmsCommerceIsolation.IsCommercePickerEntity(model.EntityType))
        {
            return NotFound();
        }

        var isCustomerPicker = model.EntityType.EqualsNoCase("customer");
        if (isCustomerPicker && !await CanAccessCustomerPicker())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        try
        {
            model.PageSize = Math.Max(Math.Min(model.PageSize, 96), 1);

            var disableIds = model.DisableIds.SplitSafe(',').Select(x => x.ToInt()).ToList();
            var selected = model.Selected.SplitSafe(',');

            using var scope = new DbContextScope(_db, autoDetectChanges: false, forceNoTracking: true);
            if (isCustomerPicker)
            {
                var customerQuery = _db.Customers
                    .AsNoTracking()
                    .AsQueryable();

                if (model.SearchTerm.HasValue())
                {
                    if (model.CustomerSearchType.EqualsNoCase("Name"))
                    {
                        customerQuery = customerQuery.ApplySearchTermFilter(model.SearchTerm);
                    }
                    else if (model.CustomerSearchType.EqualsNoCase("Email"))
                    {
                        customerQuery = customerQuery.ApplyIdentFilter(email: model.SearchTerm, userName: model.SearchTerm);
                    }
                    else if (model.CustomerSearchType.EqualsNoCase("CustomerNumber"))
                    {
                        customerQuery = customerQuery.ApplyIdentFilter(customerNumber: model.SearchTerm);
                    }
                }

                var registeredRole = await _db.CustomerRoles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SystemName == SystemCustomerRoleNames.Registered);

                var customers = await customerQuery
                    .ApplyRolesFilter([registeredRole.Id])
                    .ApplyPaging(model.PageIndex, model.PageSize)
                    .ToListAsync();

                model.SearchResult = customers
                    .Select(x =>
                    {
                        var fullName = x.GetFullName();

                        return new EntityPickerModel.SearchResultModel
                        {
                            Id = x.Id,
                            ReturnValue = x.Id.ToString(),
                            Title = x.Username.NullEmpty() ?? x.Email,
                            Summary = fullName,
                            SummaryTitle = fullName,
                            Published = true,
                            Selected = selected.Contains(x.Id.ToString()),
                            Disable = disableIds.Contains(x.Id)
                        };
                    })
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            NotifyError(ex.ToAllMessages());
        }

        return PartialView("Picker.List", model);
    }

    private async Task<bool> CanAccessCustomerPicker()
        => await HasPermission(Permissions.System.AccessBackend) && await HasPermission(Permissions.Customer.Read);

    private Task<bool> HasPermission(string permissionSystemName)
        => Services.Permissions.AuthorizeAsync(permissionSystemName);

    // INFO: The route attribute had to be added to avoid a 404 error which occurred under certain undetermined conditions.
    [HttpPost]
    [AuthorizeAdmin]
    [Permission(Permissions.System.AccessBackend)]
    [ValidateAntiForgeryToken]
    [Route("entity/patch")]
    public async Task<IActionResult> Patch(string entityName, int entityId, string propertyName, [FromBody] object value)
    {
        if (OmmCmsMode.IsEnabled(Services.ApplicationContext.Configuration) &&
            !OmmCmsCommerceIsolation.IsAllowedPatchEntity(entityName))
        {
            return NotFound();
        }

        var success = false;

        try
        {
            var patch = new EntityPatch(entityName, entityId);
            patch.Properties.Add(propertyName, value);

            if (await _db.PatchEntityAsync(patch) is not null)
            {
                await _db.SaveChangesAsync();
                success = true;
            }
        }
        catch (Exception ex)
        {
            NotifyError(ex.ToAllMessages());
        }

        return Json(new { success, entityName, entityId, propertyName });
    }

    [HttpPost]
    [AuthorizeAdmin]
    [Permission(Permissions.System.AccessBackend)]
    [ValidateAntiForgeryToken]
    [Route("entity/patchlocalized")]
    public async Task<IActionResult> PatchLocalized(int languageId, string entityName, int entityId, string propertyName, [FromBody] object value)
    {
        if (OmmCmsMode.IsEnabled(Services.ApplicationContext.Configuration) &&
            !OmmCmsCommerceIsolation.IsAllowedPatchEntity(entityName))
        {
            return NotFound();
        }

        var success = false;

        try
        {
            var localizedProperty = await _db.LocalizedProperties
                .ApplyStandardFilter(languageId, entityId, entityName, propertyName)
                .FirstOrDefaultAsync();

            _locEntityService.Value.ApplyLocalizedValue(
                localizedProperty,
                entityId,
                entityName,
                propertyName,
                value,
                languageId);

            await _db.SaveChangesAsync();
            success = true;
        }
        catch (Exception ex)
        {
            NotifyError(ex.ToAllMessages());
        }

        return Json(new { success, languageId, entityName, entityId, propertyName });
    }
}