using RxFlow.Domain;
using System.Text.Json;

namespace RxFlow.Application;

public sealed class SubmitOrderService
{
    private readonly IOrderRepository _orders;
    private readonly IPriceCalculator _pricing;
    private readonly IOrderWorkDispatcher _dispatcher;
    private readonly IOutboxWriter _outbox;

    public SubmitOrderService(IOrderRepository orders, IPriceCalculator pricing, IOrderWorkDispatcher dispatcher, IOutboxWriter outbox)
    {
        _orders = orders;
        _pricing = pricing;
        _dispatcher = dispatcher;
        _outbox = outbox;
    }

    public async Task<SubmitOrderResult> SubmitAsync(SubmitOrderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var order = new LensOrder(Guid.NewGuid(), command.Prescription, command.Frame);
        var price = await _pricing.CalculateAsync(order, cancellationToken).ConfigureAwait(false);
        if (price < 0) throw new InvalidOperationException("Price cannot be negative.");
        await _orders.AddAsync(order, cancellationToken).ConfigureAwait(false);
        await _outbox.AppendAsync("OrderSubmitted.v1", order.Id, JsonSerializer.Serialize(new { orderId = order.Id, occurredAt = DateTimeOffset.UtcNow }), cancellationToken).ConfigureAwait(false);
        await _dispatcher.DispatchAsync(order.Id, cancellationToken).ConfigureAwait(false);
        return new SubmitOrderResult(order.Id, price, order.Status);
    }
}
