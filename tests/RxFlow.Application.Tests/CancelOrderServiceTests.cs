using RxFlow.Application;
using RxFlow.Domain;
using Xunit;

namespace RxFlow.Application.Tests;

public sealed class CancelOrderServiceTests
{
    [Fact]
    public async Task CancelTransitionsSubmittedOrderToRejectedAndWritesOutboxEvent()
    {
        var order = new LensOrder(Guid.NewGuid(), new Prescription(1, 0, 90), new Frame("F-001", 50, 40));
        var orders = new FakeOrders(order);
        var outbox = new FakeOutbox();
        var service = new CancelOrderService(orders, outbox);

        var result = await service.CancelAsync(order.Id, CancellationToken.None);

        Assert.Equal(CancelOrderOutcome.Cancelled, result.Outcome);
        Assert.Equal(OrderStatus.Rejected, result.Status);
        Assert.Equal(OrderStatus.Rejected, orders.Items[order.Id].Status);
        Assert.Equal("OrderCancelled.v1", outbox.EventType);
    }

    [Fact]
    public async Task CancelReturnsNotFoundForUnknownOrder()
    {
        var service = new CancelOrderService(new FakeOrders(), new FakeOutbox());

        var result = await service.CancelAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(CancelOrderOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task CancelReturnsAlreadyCancelledForRejectedOrder()
    {
        var order = new LensOrder(Guid.NewGuid(), new Prescription(1, 0, 90), new Frame("F-001", 50, 40));
        order.TransitionTo(OrderStatus.Rejected);
        var outbox = new FakeOutbox();
        var service = new CancelOrderService(new FakeOrders(order), outbox);

        var result = await service.CancelAsync(order.Id, CancellationToken.None);

        Assert.Equal(CancelOrderOutcome.AlreadyCancelled, result.Outcome);
        Assert.Null(outbox.EventType);
    }

    [Fact]
    public async Task CancelReturnsNotCancellableForShippedOrder()
    {
        var order = new LensOrder(Guid.NewGuid(), new Prescription(1, 0, 90), new Frame("F-001", 50, 40));
        order.TransitionTo(OrderStatus.Shipped);
        var orders = new FakeOrders(order);
        var outbox = new FakeOutbox();
        var service = new CancelOrderService(orders, outbox);

        var result = await service.CancelAsync(order.Id, CancellationToken.None);

        Assert.Equal(CancelOrderOutcome.NotCancellable, result.Outcome);
        Assert.Equal(OrderStatus.Shipped, orders.Items[order.Id].Status);
        Assert.Null(outbox.EventType);
    }

    private sealed class FakeOrders : IOrderRepository
    {
        public Dictionary<Guid, LensOrder> Items { get; } = [];

        public FakeOrders(params LensOrder[] seed)
        {
            foreach (var order in seed) Items[order.Id] = order;
        }

        public Task AddAsync(LensOrder order, CancellationToken cancellationToken) { Items[order.Id] = order; return Task.CompletedTask; }
        public Task<LensOrder?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.TryGetValue(id, out var order) ? order : null);
        public Task UpdateAsync(LensOrder order, CancellationToken cancellationToken) { Items[order.Id] = order; return Task.CompletedTask; }
    }

    private sealed class FakeOutbox : IOutboxWriter
    {
        public string? EventType { get; private set; }
        public Task AppendAsync(string eventType, Guid aggregateId, string payload, CancellationToken cancellationToken) { EventType = eventType; return Task.CompletedTask; }
    }
}
