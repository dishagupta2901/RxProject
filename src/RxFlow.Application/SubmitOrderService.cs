using RxFlow.Domain;

namespace RxFlow.Application;

public sealed class SubmitOrderService
{
    private readonly IOrderRepository _orders;
    private readonly IPriceCalculator _pricing;
    private readonly IOrderWorkDispatcher _dispatcher;

    public SubmitOrderService(IOrderRepository orders, IPriceCalculator pricing, IOrderWorkDispatcher dispatcher)
    {
        _orders = orders;
        _pricing = pricing;
        _dispatcher = dispatcher;
    }

    public async Task<SubmitOrderResult> SubmitAsync(SubmitOrderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var order = new LensOrder(Guid.NewGuid(), command.Prescription, command.Frame);
        var price = await _pricing.CalculateAsync(order, cancellationToken).ConfigureAwait(false);
        if (price < 0) throw new InvalidOperationException("Price cannot be negative.");
        await _orders.AddAsync(order, cancellationToken).ConfigureAwait(false);
        await _dispatcher.DispatchAsync(order.Id, cancellationToken).ConfigureAwait(false);
        return new SubmitOrderResult(order.Id, price, order.Status);
    }
}
