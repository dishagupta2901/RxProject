namespace RxFlow.Reporting;

/// <summary>
/// The one sanctioned read boundary for order status. Adapters (EF/PostgreSQL for the persisted
/// deployment, an in-memory adapter for local/demo mode) implement this against whatever store is
/// currently authoritative; callers never see storage internals.
/// </summary>
public interface IOrderReportReader
{
    Task<OrderStatusView?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns up to <paramref name="take"/> orders. Ordering is best-effort: no submitted-at
    /// column exists on the order write model yet, so callers must not assume recency ordering
    /// (tracked as a follow-up; see architecture.md).
    /// </summary>
    Task<IReadOnlyList<OrderStatusView>> ListOrdersAsync(int take, CancellationToken cancellationToken);
}

public sealed class OrderReportingService(IOrderReportReader reader)
{
    private const int MaxTake = 200;

    public Task<OrderStatusView?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        reader.GetOrderAsync(orderId, cancellationToken);

    public Task<IReadOnlyList<OrderStatusView>> ListOrdersAsync(int take, CancellationToken cancellationToken)
    {
        if (take is < 1 or > MaxTake) throw new ArgumentOutOfRangeException(nameof(take), take, $"take must be between 1 and {MaxTake}.");
        return reader.ListOrdersAsync(take, cancellationToken);
    }
}
