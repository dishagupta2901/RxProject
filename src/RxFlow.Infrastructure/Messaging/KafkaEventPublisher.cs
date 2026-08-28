using System.Text.Json;
using Confluent.Kafka;

namespace RxFlow.Infrastructure.Messaging;

public sealed class KafkaEventPublisher(IProducer<string, string> producer, string topic)
{
    public Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var message = new Message<string, string>
        {
            Key = envelope.OrderId,
            Value = JsonSerializer.Serialize(envelope)
        };
        return producer.ProduceAsync(topic, message, cancellationToken);
    }
}
