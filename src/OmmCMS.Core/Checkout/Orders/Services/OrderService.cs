using OmmCMS.Core.Common;
using OmmCMS.Core.Common.Services;
using OmmCMS.Core.Data;
using OmmCMS.Data.Hooks;
using OmmCMS.Events;

namespace OmmCMS.Core.Checkout.Orders;

public partial class OrderService : AsyncDbSaveHook<Order>, IOrderService
{
    private readonly IWorkContext _workContext;
    private readonly IEventPublisher _eventPublisher;

    public OrderService(
        IWorkContext workContext,
        IEventPublisher eventPublisher)
    {
        _workContext = workContext;
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

    public Task<(Money OrderTotal, Money RoundingAmount)> GetOrderTotalInCustomerCurrencyAsync(
        Order order,
        Currency targetCurrency)
    {
        Guard.NotNull(order);

        var roundingAmount = order.OrderTotalRounding;
        var orderTotal = order.OrderTotal * order.CurrencyRate;

        // Currency for output.
        var currency = targetCurrency ?? _workContext.WorkingCurrency;

        return Task.FromResult((new Money(orderTotal, currency), new Money(roundingAmount, currency)));
    }
}
