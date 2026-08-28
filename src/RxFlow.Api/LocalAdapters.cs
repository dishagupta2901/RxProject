using System.Collections.Concurrent;
using RxFlow.Application;
using RxFlow.Domain;
using RxFlow.Reporting;

namespace RxFlow.Api;

/// <summary>
/// In-memory stand-in for both the write-side order repository and the reporting read reader in
/// local/demo mode (<c>Persistence:ApplyMigrations=false</c>). Both interfaces are backed by the
/// same dictionary here only because there is no separate persistence layer to read from yet in
/// that mode; once PostgreSQL is the live store (<c>Persistence:ApplyMigrations=true</c>), the two
/// concerns are served by genuinely separate adapters (<c>EfOrderRepository</c> and
/// <c>EfOrderReportReader</c>) with no shared internals. See Program.cs for the switch.
/// </summary>
internal sealed class LocalOrderRepository : IOrderRepository, IOrderReportReader
{
    private readonly ConcurrentDictionary<Guid, LensOrder> _orders = new();

    public Task AddAsync(LensOrder order, CancellationToken cancellationToken)
    { _orders[order.Id] = order; return Task.CompletedTask; }

    public Task<LensOrder?> GetAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(_orders.TryGetValue(id, out var order) ? order : null);

    public Task UpdateAsync(LensOrder order, CancellationToken cancellationToken)
    { _orders[order.Id] = order; return Task.CompletedTask; }

    Task<OrderStatusView?> IOrderReportReader.GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
        => Task.FromResult(_orders.TryGetValue(orderId, out var order) ? ToView(order) : null);

    Task<IReadOnlyList<OrderStatusView>> IOrderReportReader.ListOrdersAsync(int take, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<OrderStatusView>>(_orders.Values.Take(take).Select(ToView).ToList());

    private static OrderStatusView ToView(LensOrder order) =>
        new(order.Id, order.Status, order.Frame.Id, order.Prescription.Sphere, order.Prescription.Cylinder, order.Prescription.Axis);
}

internal sealed class LocalPriceCalculator : IPriceCalculator
{
    public Task<decimal> CalculateAsync(LensOrder order, CancellationToken cancellationToken)
        => Task.FromResult(100m + Math.Abs(order.Prescription.Cylinder) * 10m);
}

internal sealed class LocalWorkDispatcher : IOrderWorkDispatcher
{
    public Task DispatchAsync(Guid orderId, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class LocalOutboxWriter : IOutboxWriter
{
    public Task AppendAsync(string eventType, Guid aggregateId, string payload, CancellationToken cancellationToken) => Task.CompletedTask;
}
