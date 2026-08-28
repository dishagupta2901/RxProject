using Microsoft.EntityFrameworkCore;
using RxFlow.Domain;
using RxFlow.Infrastructure.Persistence;
using RxFlow.Reporting;

namespace RxFlow.Infrastructure.Reporting;

/// <summary>
/// Read-only, no-tracking projection over the PostgreSQL order store. Never returns the tracked
/// write-model <see cref="LensOrder"/> entity and never calls SaveChanges, so it cannot be used to
/// mutate order state.
/// </summary>
public sealed class EfOrderReportReader(RxFlowDbContext db) : IOrderReportReader
{
    public async Task<OrderStatusView?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            .ConfigureAwait(false);
        return order is null ? null : ToView(order);
    }

    public async Task<IReadOnlyList<OrderStatusView>> ListOrdersAsync(int take, CancellationToken cancellationToken)
    {
        var orders = await db.Orders.AsNoTracking()
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return orders.ConvertAll(ToView);
    }

    private static OrderStatusView ToView(LensOrder order) =>
        new(order.Id, order.Status, order.Frame.Id, order.Prescription.Sphere, order.Prescription.Cylinder, order.Prescription.Axis);
}
