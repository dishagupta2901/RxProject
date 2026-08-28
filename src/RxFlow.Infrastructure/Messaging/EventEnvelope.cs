namespace RxFlow.Infrastructure.Messaging;

public sealed record EventEnvelope(string EventType, string OrderId, DateTimeOffset OccurredAt, int Version = 1);
