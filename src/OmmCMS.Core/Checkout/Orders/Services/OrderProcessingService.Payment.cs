using OmmCMS.Core.Checkout.Payment;
using OmmCMS.Core.Common;
using OmmCMS.Core.Identity;

namespace OmmCMS.Core.Checkout.Orders;

public partial class OrderProcessingService : IOrderProcessingService
{
    public virtual async Task MarkAsAuthorizedAsync(Order order)
    {
        Guard.NotNull(order);

        order.PaymentStatusId = (int)PaymentStatus.Authorized;

        AddOrderNotes(order, T("Admin.OrderNotice.OrderMarkedAsAuthorized"));

        // INFO: CheckOrderStatus performs commit.
        await CheckOrderStatusAsync(order);
    }

    public virtual async Task MarkOrderAsPaidAsync(Order order)
    {
        Guard.NotNull(order);

        if (!order.CanMarkOrderAsPaid())
        {
            throw new PaymentException(T("Order.CannotMarkPaid"));
        }

        order.PaymentStatusId = (int)PaymentStatus.Paid;
        order.PaidDateUtc = DateTime.UtcNow;

        AddOrderNotes(order, T("Admin.OrderNotice.OrderMarkedAsPaid"));

        // INFO: CheckOrderStatus performs commit.
        await CheckOrderStatusAsync(order);

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            await _eventPublisher.PublishOrderPaidAsync(order);
        }
    }

    public virtual async Task<bool> CanCaptureAsync(Order order)
    {
        Guard.NotNull(order);

        if (order.OrderStatus == OrderStatus.Cancelled || order.OrderStatus == OrderStatus.Pending)
            return false;

        if (order.PaymentStatus == PaymentStatus.Authorized && await _paymentService.SupportCaptureAsync(order.PaymentMethodSystemName))
            return true;

        return false;
    }

    public virtual async Task CaptureAsync(Order order)
    {
        Guard.NotNull(order);

        if (!await CanCaptureAsync(order))
        {
            throw new PaymentException(T("Order.CannotCapture"));
        }

        try
        {
            var result = await _paymentService.CaptureAsync(new()
            {
                Order = order
            });

            order.CaptureTransactionId = result.CaptureTransactionId;
            order.CaptureTransactionResult = result.CaptureTransactionResult.Truncate(400);
            order.PaymentStatus = result.NewPaymentStatus;
            order.PaidDateUtc = result.NewPaymentStatus == PaymentStatus.Paid ? DateTime.UtcNow : order.PaidDateUtc;

            AddOrderNotes(order, T("Admin.OrderNotice.OrderCaptured"));

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                await _eventPublisher.PublishOrderPaidAsync(order);
            }
        }
        catch (Exception ex)
        {
            AddOrderNotes(order, T("Admin.OrderNotice.OrderCaptureError", order.GetOrderNumber()).ToString() + " " + ex.Message);
            await _db.SaveChangesAsync();
            ex.ReThrow();
        }
    }

    public virtual async Task<bool> CanRefundAsync(Order order)
    {
        Guard.NotNull(order);

        if (order.OrderTotal == decimal.Zero)
            return false;

        // Only partial refunds allowed if already refunded.
        if (order.RefundedAmount > decimal.Zero)
            return false;

        // Uncomment the lines below in order to allow this operation for cancelled orders.
        //if (order.OrderStatus == OrderStatus.Cancelled)
        //    return false;

        if (order.PaymentStatus == PaymentStatus.Paid && await _paymentService.SupportRefundAsync(order.PaymentMethodSystemName))
            return true;

        return false;
    }

    public virtual async Task RefundAsync(Order order)
    {
        Guard.NotNull(order);

        if (!await CanRefundAsync(order))
        {
            throw new PaymentException(T("Order.CannotRefund"));
        }

        try
        {
            var request = new RefundPaymentRequest
            {
                Order = order,
                AmountToRefund = new Money(order.OrderTotal, _primaryCurrency),
                IsPartialRefund = false
            };

            var result = await _paymentService.RefundAsync(request);
            var totalAmountRefunded = order.RefundedAmount + order.OrderTotal;

            order.RefundedAmount = totalAmountRefunded;
            order.PaymentStatus = result.NewPaymentStatus;

            AddOrderNotes(order, T("Admin.OrderNotice.OrderRefunded", request.AmountToRefund.ToString(true)));

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);
        }
        catch (Exception ex)
        {
            AddOrderNotes(order, T("Admin.OrderNotice.OrderRefundError", order.GetOrderNumber()).ToString() + " " + ex.Message);
            await _db.SaveChangesAsync();
            ex.ReThrow();
        }
    }

    public virtual async Task RefundOfflineAsync(Order order)
    {
        Guard.NotNull(order);

        if (!order.CanRefundOffline())
        {
            throw new PaymentException(T("Order.CannotRefund"));
        }

        var amountToRefund = new Money(order.OrderTotal, _primaryCurrency);
        var totalAmountRefunded = order.RefundedAmount + amountToRefund.Amount;

        order.RefundedAmount = totalAmountRefunded;
        order.PaymentStatus = PaymentStatus.Refunded;

        AddOrderNotes(order, T("Admin.OrderNotice.OrderMarkedAsRefunded", amountToRefund.ToString(true)));

        // INFO: CheckOrderStatus performs commit.
        await CheckOrderStatusAsync(order);
    }

    public virtual async Task<bool> CanPartiallyRefundAsync(Order order, decimal amountToRefund)
    {
        Guard.NotNull(order);

        if (order.OrderTotal == decimal.Zero)
            return false;

        // Uncomment the lines below in order to allow this operation for cancelled orders.
        //if (order.OrderStatus == OrderStatus.Cancelled)
        //    return false;

        var canBeRefunded = order.OrderTotal - order.RefundedAmount;
        if (canBeRefunded <= decimal.Zero)
            return false;

        if (amountToRefund > canBeRefunded)
            return false;

        if ((order.PaymentStatus == PaymentStatus.Paid || order.PaymentStatus == PaymentStatus.PartiallyRefunded) &&
            await _paymentService.SupportPartiallyRefundAsync(order.PaymentMethodSystemName))
            return true;

        return false;
    }

    public virtual async Task PartiallyRefundAsync(Order order, decimal amountToRefund)
    {
        Guard.NotNull(order);

        if (!await CanPartiallyRefundAsync(order, amountToRefund))
        {
            throw new PaymentException(T("Order.CannotPartialRefund"));
        }

        try
        {
            var request = new RefundPaymentRequest
            {
                Order = order,
                AmountToRefund = new Money(amountToRefund, _primaryCurrency),
                IsPartialRefund = true
            };

            var result = await _paymentService.RefundAsync(request);

            var totalAmountRefunded = order.RefundedAmount + amountToRefund;
            order.RefundedAmount = totalAmountRefunded;
            order.PaymentStatus = result.NewPaymentStatus;

            AddOrderNotes(order, T("Admin.OrderNotice.OrderPartiallyRefunded", request.AmountToRefund.ToString(true)));

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);
        }
        catch (Exception ex)
        {
            AddOrderNotes(order, T("Admin.OrderNotice.OrderPartiallyRefundError", order.GetOrderNumber()).ToString() + " " + ex.Message);
            await _db.SaveChangesAsync();
            ex.ReThrow();
        }
    }

    public virtual async Task PartiallyRefundOfflineAsync(Order order, decimal amountToRefund)
    {
        Guard.NotNull(order);

        if (!order.CanPartiallyRefundOffline(amountToRefund))
        {
            throw new PaymentException(T("Order.CannotPartialRefund"));
        }

        var totalAmountRefunded = order.RefundedAmount + amountToRefund;
        order.RefundedAmount = totalAmountRefunded;
        order.PaymentStatus = PaymentStatus.PartiallyRefunded;

        var formattedAmount = new Money(amountToRefund, _primaryCurrency).ToString(true);
        AddOrderNotes(order, T("Admin.OrderNotice.OrderMarkedAsPartiallyRefunded", formattedAmount));

        // INFO: CheckOrderStatus performs commit.
        await CheckOrderStatusAsync(order);
    }

    public virtual async Task<bool> CanVoidAsync(Order order)
    {
        Guard.NotNull(order);

        if (order.OrderTotal == decimal.Zero)
            return false;

        // Uncomment the lines below in order to allow this operation for cancelled orders.
        //if (order.OrderStatus == OrderStatus.Cancelled)
        //    return false;

        if (order.PaymentStatus == PaymentStatus.Authorized && await _paymentService.SupportVoidAsync(order.PaymentMethodSystemName))
            return true;

        return false;
    }

    public virtual async Task VoidAsync(Order order)
    {
        Guard.NotNull(order);

        if (!await CanVoidAsync(order))
        {
            throw new PaymentException(T("Order.CannotVoid"));
        }

        try
        {
            var result = await _paymentService.VoidAsync(new()
            {
                Order = order
            });

            order.PaymentStatus = result.NewPaymentStatus;
            AddOrderNotes(order, T("Admin.OrderNotice.OrderVoided"));

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);
        }
        catch (Exception ex)
        {
            AddOrderNotes(order, T("Admin.OrderNotice.OrderVoidError", order.GetOrderNumber()).ToString() + " " + ex.Message);
            await _db.SaveChangesAsync();
            ex.ReThrow();
        }
    }

    public virtual async Task VoidOfflineAsync(Order order)
    {
        Guard.NotNull(order);

        if (!order.CanVoidOffline())
        {
            throw new PaymentException(T("Order.CannotVoid"));
        }

        order.PaymentStatusId = (int)PaymentStatus.Voided;
        AddOrderNotes(order, T("Admin.OrderNotice.OrderMarkedAsVoided"));

        // INFO: CheckOrderStatus performs commit.
        await CheckOrderStatusAsync(order);
    }

    public virtual async Task<bool> IsDuplicatePaymentReferenceAsync(Exception ex)
    {
        if (ex is DbUpdateException dbEx && _db.DataProvider.IsUniquenessViolationException(dbEx))
        {
            var paymentReferenceHashCode = dbEx.Entries
                .Select(x => x.Entity)
                .OfType<Order>()
                .Select(x => x.PaymentReferenceHashCode)
                .FirstOrDefault(x => x.HasValue);

            return paymentReferenceHashCode.HasValue &&
                await _db.Orders
                    .IgnoreQueryFilters()
                    .AnyAsync(x => x.PaymentReferenceHashCode == paymentReferenceHashCode);
        }

        return false;
    }
}
