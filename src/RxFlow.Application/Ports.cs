using RxFlow.Domain;

namespace RxFlow.Application;

public interface IOrderRepository
{
    Task AddAsync(LensOrder order, CancellationToken cancellationToken);
    Task<LensOrder?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public interface IOrderWorkDispatcher
{
    Task DispatchAsync(Guid orderId, CancellationToken cancellationToken);
}

public interface IPriceCalculator
{
    Task<decimal> CalculateAsync(LensOrder order, CancellationToken cancellationToken);
}

public sealed record SubmitOrderCommand(Prescription Prescription, Frame Frame);
public sealed record SubmitOrderResult(Guid OrderId, decimal Price, OrderStatus Status);
