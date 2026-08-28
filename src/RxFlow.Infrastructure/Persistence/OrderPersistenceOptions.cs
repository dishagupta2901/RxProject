namespace RxFlow.Infrastructure.Persistence;

public sealed class OrderPersistenceOptions
{
    public const string SectionName = "Persistence";
    public string ConnectionString { get; init; } = string.Empty;
}
