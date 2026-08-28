using RxFlow.Application;
using RxFlow.Domain;

namespace RxFlow.Workers;

public sealed class OrderWorkflowJob
{
    private readonly IOrderRepository _orders;

    public OrderWorkflowJob(IOrderRepository orders) => _orders = orders;

    public async Task ExecuteAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _orders.GetAsync(orderId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Order {orderId} was not found.");

        if (order.Status == OrderStatus.Submitted)
        {
            order.ValidateGrindability(maxAbsolutePower: 12m);
        }
    }
}
