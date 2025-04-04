using Confluent.Kafka;
using System.Threading.Tasks;

public class KafkaService
{
    private readonly ProducerConfig _producerConfig;

    public KafkaService(string bootstrapServers)
    {
        _producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };
    }

    public async Task ProduceAsync(string topic, string message)
    {
        using var producer = new ProducerBuilder<Null, string>(_producerConfig).Build();
        await producer.ProduceAsync(topic, new Message<Null, string> { Value = message });
    }
}