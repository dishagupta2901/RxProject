using RxFlow.Application;

namespace RxFlow.Infrastructure.Persistence;

public sealed class EfOutboxWriter(RxFlowDbContext db) : IOutboxWriter
{
    public Task AppendAsync(string eventType, Guid aggregateId, string payload, CancellationToken cancellationToken)
    {
        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), EventType = eventType, AggregateId = aggregateId.ToString(), Payload = payload, OccurredAt = DateTimeOffset.UtcNow });
        return db.SaveChangesAsync(cancellationToken);
    }
}
