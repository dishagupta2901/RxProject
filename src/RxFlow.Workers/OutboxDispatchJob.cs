using RxFlow.Infrastructure.Messaging;

namespace RxFlow.Workers;

public sealed class OutboxDispatchJob(OutboxDispatcher dispatcher)
{
    public Task<int> ExecuteAsync(CancellationToken cancellationToken)
        => dispatcher.DispatchBatchAsync(100, cancellationToken);
}
