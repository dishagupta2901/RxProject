using StackExchange.Redis;

namespace RxFlow.Infrastructure.Coordination;

public sealed class RedisOrderCache(IConnectionMultiplexer connection) : IOrderCache
{
    private readonly IDatabase _database = connection.GetDatabase();

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken)
        => _database.StringGetAsync(key).ContinueWith(x => (string?)x.Result, cancellationToken);

    public Task SetAsync(string key, string value, TimeSpan lifetime, CancellationToken cancellationToken)
        => _database.StringSetAsync(key, value, lifetime);
}
