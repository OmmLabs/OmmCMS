using OmmCMS.Core.Checkout.Payment;
using OmmCMS.Core.Common;
using OmmCMS.Core.Common.Services;
using OmmCMS.Core.Data;
using OmmCMS.Data.Hooks;
using OmmCMS.Events;

namespace OmmCMS.Core.Checkout.Orders;

public partial class OrderService : AsyncDbSaveHook<Order>, IOrderService
{
    private readonly SmartDbContext _db;
    private readonly IWorkContext _workContext;
    private readonly IRoundingHelper _roundingHelper;
    private readonly IEventPublisher _eventPublisher;

    public OrderService(
        SmartDbContext db,
        IWorkContext workContext,
        IRoundingHelper roundingHelper,
        IEventPublisher eventPublisher)
    {
        _db = db;
        _workContext = workContext;
        _roundingHelper = roundingHelper;
        _eventPublisher = eventPublisher;
    }

    #region Hook

    protected override Task<HookResult> OnUpdatingAsync(Order entity, IHookedEntity entry, CancellationToken cancelToken)
    {
        if (entity.OrderStatus == OrderStatus.Complete && entity.CompletedOn == null)
        {
            entity.CompletedOn = DateTime.UtcNow;
        }

        return Task.FromResult(HookResult.Ok);
    }

    protected override Task<HookResult> OnUpdatedAsync(Order entity, IHookedEntity entry, CancellationToken cancelToken)
        => Task.FromResult(HookResult.Ok);

    public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
    {
        var orders = entries
            .Select(x => x.Entity)
            .OfType<Order>()
            .ToList();

        foreach (var order in orders)
        {
            await _eventPublisher.PublishOrderUpdatedAsync(order);
        }
    }

    #endregion

    public async Task<(Money OrderTotal, Money RoundingAmount)> GetOrderTotalInCustomerCurrencyAsync(
        Order order,
        Currency targetCurrency,
        PaymentMethod paymentMethod = null)
    {
        Guard.NotNull(order);

        var roundingAmount = order.OrderTotalRounding;
        var orderTotal = order.OrderTotal * order.CurrencyRate;

        // Avoid rounding the rounded order total again. This would result in a "RoundingAmount" of 0.
        if (orderTotal != order.OrderTotal &&
            targetCurrency != null &&
            targetCurrency.RoundOrderTotalEnabled &&
            order.PaymentMethodSystemName.HasValue())
        {
            paymentMethod ??= await _db.PaymentMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PaymentMethodSystemName == order.PaymentMethodSystemName);

            if (paymentMethod?.RoundOrderTotalEnabled ?? false)
            {
                orderTotal = _roundingHelper.ToNearest(orderTotal, out roundingAmount, targetCurrency);
            }
        }

        // Currency for output.
        var currency = targetCurrency ?? _workContext.WorkingCurrency;

        return (new(orderTotal, currency), new(roundingAmount, currency));
    }
}
