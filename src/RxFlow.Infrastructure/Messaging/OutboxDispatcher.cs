using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RxFlow.Infrastructure.Persistence;

namespace RxFlow.Infrastructure.Messaging;

public sealed class OutboxDispatcher(RxFlowDbContext db, KafkaEventPublisher publisher)
{
    public async Task<int> DispatchBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        var messages = await db.OutboxMessages
            .Where(x => x.DispatchedAt == null)
            .OrderBy(x => x.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var message in messages)
        {
            var envelope = JsonSerializer.Deserialize<EventEnvelope>(message.Payload)
                ?? throw new InvalidOperationException($"Outbox message {message.Id} has invalid payload.");
            await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
            message.DispatchedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return messages.Count;
    }
}
