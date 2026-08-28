using Hangfire;
using RxFlow.Application;

namespace RxFlow.Workers;

public sealed class HangfireOrderWorkDispatcher(IBackgroundJobClient jobs) : IOrderWorkDispatcher
{
    public Task DispatchAsync(Guid orderId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        jobs.Enqueue<OrderWorkflowJob>(job => job.ExecuteAsync(orderId, CancellationToken.None));
        return Task.CompletedTask;
    }
}
