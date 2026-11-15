using System.Net.Mail;
using OmmCMS.Admin.Infrastructure.Dashboard;
using OmmCMS.Admin.Models.Customers;
using OmmCMS.Core.Identity;
using OmmCMS.Core.Security;
using OmmCMS.Core.Messaging;

namespace OmmCMS.Admin.Components;

/// <summary>
/// Renders recently recorded contact messages without inspecting their subjects.
/// </summary>
public class DashboardContactRequestsViewComponent : SmartViewComponent
{
    /// <summary>
    /// Gets the data permission required to display contact messages.
    /// </summary>
    public const string RequiredPermission = Permissions.Dashboard.Widgets.Customers.ContactRequests;

    private readonly SmartDbContext _db;

    public DashboardContactRequestsViewComponent(SmartDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Loads the newest marked contact messages from authorized stores.
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync(int count = 5)
    {
        if (!await Services.Permissions.AuthorizeAsync(RequiredPermission))
            return Empty();

        var authorizedStoreIds = await Services.StoreMappingService.GetAuthorizedStoreIdsAsync(nameof(Customer), Services.WorkContext.CurrentCustomer.Id);
        var storeIds = _db.Stores.Select(x => x.Id);
        var query = _db.GenericAttributes
            .AsNoTracking()
            .Where(x => x.KeyGroup == nameof(QueuedEmail)
                && x.Key == ContactRequestsDashboardHook.AttributeKey
                && storeIds.Contains(x.StoreId));

        if (authorizedStoreIds.Length > 0)
        {
            query = query.Where(x => authorizedStoreIds.Contains(x.StoreId));
        }

        // TODO: (mh) I don't get it. QueuedEmails are deleted after sending.
        var messages = await _db.QueuedEmails
            .AsNoTracking()
            .Join(query,
                message => message.Id,
                attribute => attribute.EntityId,
                (message, attribute) => new { message.Id, Email = attribute.Value, message.CreatedOnUtc })
            .OrderByDescending(x => x.CreatedOnUtc)
            .ThenByDescending(x => x.Id)
            .Take(Math.Clamp(count, 1, 50))
            .ToListAsync();

        var model = messages.Select(x => new DashboardContactRequestModel
        {
            MessageId = x.Id,
            Email = MailAddress.TryCreate(x.Email, out var sender) ? sender.Address : string.Empty,
            CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc).ToString("g")
        }).ToList();

        return View(model);
    }
}
