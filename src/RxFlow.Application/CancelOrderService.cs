using RxFlow.Domain;
using System.Text.Json;

namespace RxFlow.Application;

/// <summary>
/// Cancels a submitted order by transitioning it to <see cref="OrderStatus.Rejected"/>.
/// A shipped order cannot be cancelled, and cancelling an already-cancelled order is
/// reported as <see cref="CancelOrderOutcome.AlreadyCancelled"/> rather than an error.
/// </summary>
public sealed class CancelOrderService
{
    private readonly IOrderRepository _orders;
    private readonly IOutboxWriter _outbox;

    public CancelOrderService(IOrderRepository orders, IOutboxWriter outbox)
    {
        _orders = orders;
        _outbox = outbox;
    }

    public async Task<CancelOrderResult> CancelAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _orders.GetAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null) return new CancelOrderResult(CancelOrderOutcome.NotFound, null);
        if (order.Status == OrderStatus.Rejected) return new CancelOrderResult(CancelOrderOutcome.AlreadyCancelled, order.Status);
        if (order.Status == OrderStatus.Shipped) return new CancelOrderResult(CancelOrderOutcome.NotCancellable, order.Status);

        order.TransitionTo(OrderStatus.Rejected);
        await _orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync("OrderCancelled.v1", order.Id, JsonSerializer.Serialize(new { orderId = order.Id, occurredAt = DateTimeOffset.UtcNow }), cancellationToken).ConfigureAwait(false);
        return new CancelOrderResult(CancelOrderOutcome.Cancelled, order.Status);
    }
}
