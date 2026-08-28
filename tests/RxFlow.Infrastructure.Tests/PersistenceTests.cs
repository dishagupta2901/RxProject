using Microsoft.EntityFrameworkCore;
using RxFlow.Domain;
using RxFlow.Infrastructure.Persistence;
using RxFlow.Infrastructure.Reporting;
using Xunit;

namespace RxFlow.Infrastructure.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task OrderRepositoryRoundTripsOrder()
    {
        var options = new DbContextOptionsBuilder<RxFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new RxFlowDbContext(options);
        var repository = new EfOrderRepository(db);
        var order = new LensOrder(Guid.NewGuid(), new Prescription(1, 0, 90), new Frame("F-001", 50, 40));

        await repository.AddAsync(order, CancellationToken.None);
        var loaded = await repository.GetAsync(order.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(order.Id, loaded!.Id);
        Assert.Equal(order.Frame.Id, loaded.Frame.Id);
    }

    [Fact]
    public async Task OrderRepositoryPersistsUpdateAcrossDbContextInstances()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<RxFlowDbContext>().UseInMemoryDatabase(databaseName).Options;
        var order = new LensOrder(Guid.NewGuid(), new Prescription(1, 0, 90), new Frame("F-001", 50, 40));

        await using (var writeDb = new RxFlowDbContext(options))
        {
            var repository = new EfOrderRepository(writeDb);
            await repository.AddAsync(order, CancellationToken.None);

            var loaded = await repository.GetAsync(order.Id, CancellationToken.None);
            loaded!.TransitionTo(OrderStatus.Validated);
            await repository.UpdateAsync(loaded, CancellationToken.None);
        }

        await using var readDb = new RxFlowDbContext(options);
        var reloaded = await new EfOrderRepository(readDb).GetAsync(order.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(OrderStatus.Validated, reloaded!.Status);
    }

    [Fact]
    public async Task ReportReaderProjectsPersistedOrderWithoutTrackingIt()
    {
        var options = new DbContextOptionsBuilder<RxFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new RxFlowDbContext(options);
        var order = new LensOrder(Guid.NewGuid(), new Prescription(-2.5m, 0.75m, 90), new Frame("F-002", 52, 38));
        await new EfOrderRepository(db).AddAsync(order, CancellationToken.None);

        var reader = new EfOrderReportReader(db);
        var view = await reader.GetOrderAsync(order.Id, CancellationToken.None);
        var list = await reader.ListOrdersAsync(take: 10, CancellationToken.None);

        Assert.NotNull(view);
        Assert.Equal(order.Id, view!.OrderId);
        Assert.Equal(OrderStatus.Submitted, view.Status);
        Assert.Equal(order.Frame.Id, view.FrameId);
        Assert.Equal(order.Prescription.Sphere, view.Sphere);
        Assert.Single(list);
        Assert.Single(db.ChangeTracker.Entries<LensOrder>()); // only the write-side Add above is tracked, not the read
    }

    [Fact]
    public async Task ReportReaderReturnsNullForUnknownOrder()
    {
        var options = new DbContextOptionsBuilder<RxFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new RxFlowDbContext(options);
        var reader = new EfOrderReportReader(db);

        var view = await reader.GetOrderAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(view);
    }
}
