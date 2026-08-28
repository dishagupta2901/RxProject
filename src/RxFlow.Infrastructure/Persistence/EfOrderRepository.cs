using Microsoft.EntityFrameworkCore;
using RxFlow.Application;
using RxFlow.Domain;

namespace RxFlow.Infrastructure.Persistence;

public sealed class EfOrderRepository(RxFlowDbContext db) : IOrderRepository
{
    public async Task AddAsync(LensOrder order, CancellationToken cancellationToken)
    {
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<LensOrder?> GetAsync(Guid id, CancellationToken cancellationToken)
        => db.Orders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}
