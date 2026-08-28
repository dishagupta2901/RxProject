using RxFlow.Application;
using RxFlow.Domain;
using Xunit;

namespace RxFlow.Application.Tests;

public sealed class SubmitOrderServiceTests
{
    [Fact]
    public async Task SubmitPersistsDispatchesAndReturnsPrice()
    {
        var orders = new FakeOrders();
        var dispatcher = new FakeDispatcher();
        var service = new SubmitOrderService(orders, new FixedPricing(125m), dispatcher, new FakeOutbox());

        var result = await service.SubmitAsync(new SubmitOrderCommand(new Prescription(1, 0, 90), new Frame("F-001", 50, 40)), CancellationToken.None);

        Assert.Equal(125m, result.Price);
        Assert.Single(orders.Items);
        Assert.Equal(result.OrderId, dispatcher.OrderId);
    }

    [Fact]
    public async Task SubmitRejectsNegativePriceBeforePersisting()
    {
        var orders = new FakeOrders();
        var service = new SubmitOrderService(orders, new FixedPricing(-1m), new FakeDispatcher(), new FakeOutbox());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitAsync(new SubmitOrderCommand(new Prescription(1, 0, 90), new Frame("F-001", 50, 40)), CancellationToken.None));
        Assert.Empty(orders.Items);
    }

    private sealed class FixedPricing(decimal value) : IPriceCalculator
    {
        public Task<decimal> CalculateAsync(LensOrder order, CancellationToken cancellationToken) => Task.FromResult(value);
    }

    private sealed class FakeOrders : IOrderRepository
    {
        public List<LensOrder> Items { get; } = [];
        public Task AddAsync(LensOrder order, CancellationToken cancellationToken) { Items.Add(order); return Task.CompletedTask; }
        public Task<LensOrder?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
    }

    private sealed class FakeDispatcher : IOrderWorkDispatcher
    {
        public Guid OrderId { get; private set; }
        public Task DispatchAsync(Guid orderId, CancellationToken cancellationToken) { OrderId = orderId; return Task.CompletedTask; }
    }

    private sealed class FakeOutbox : IOutboxWriter
    {
        public Task AppendAsync(string eventType, Guid aggregateId, string payload, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
