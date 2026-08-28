namespace RxFlow.Infrastructure.Coordination;

public interface IOrderCache
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);
    Task SetAsync(string key, string value, TimeSpan lifetime, CancellationToken cancellationToken);
}

public interface IOrderLock
{
    Task<IAsyncDisposable?> AcquireAsync(string key, TimeSpan lease, CancellationToken cancellationToken);
}
