using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Threading;

public class KafkaService
{
    private readonly ProducerConfig _producerConfig;
    private readonly ILogger<KafkaService> _logger;

    public KafkaService(string bootstrapServers, ILogger<KafkaService> logger)
    {
        _producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };
        _logger = logger;
    }

    public async Task ProduceAsync(string topic, string message)
    {
        using var producer = new ProducerBuilder<Null, string>(_producerConfig).Build();
        _logger.LogInformation($"Producing message to topic {topic}: {message}");
        await producer.ProduceAsync(topic, new Message<Null, string> { Value = message });
    }

    private bool TopicExists(string topic)
    {
        var adminConfig = new AdminClientConfig { BootstrapServers = _producerConfig.BootstrapServers };
        using var adminClient = new AdminClientBuilder(adminConfig).Build();

        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            foreach (var topicMetadata in metadata.Topics)
            {
                if (topicMetadata.Topic == topic)
                {
                    _logger.LogInformation($"Topic '{topic}' exists.");
                    return true;
                }
            }
            _logger.LogWarning($"Topic '{topic}' does not exist.");
            return false;
        }
        catch (KafkaException ex)
        {
            _logger.LogError($"Error while checking topic existence: {ex.Message}");
            return false;
        }
    }

    public async Task ConsumeMessagesAsync(string topic)
    {
        if (!TopicExists(topic))
        {
            _logger.LogError($"Cannot consume messages. Topic '{topic}' does not exist.");
            return;
        }

        //Call producer method to produce a message
        await ProduceAsync(topic, "Test message from KafkaService.");

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _producerConfig.BootstrapServers,
            GroupId = Guid.NewGuid().ToString(), // Usar un GroupId único para ignorar offsets previos
            AutoOffsetReset = AutoOffsetReset.Earliest, // Leer desde el inicio del tópico
            EnableAutoCommit = false // Deshabilitar el auto-commit para evitar guardar offsets
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        // Note: ConsumerBuilder.Build() does not return null. Null checks are only needed for consumeResult.
        _logger.LogInformation($"BootstrapServers: {consumerConfig.BootstrapServers}");
        _logger.LogInformation($"GroupId: {consumerConfig.GroupId}");
        _logger.LogInformation($"AutoOffsetReset: {consumerConfig.AutoOffsetReset}");
        _logger.LogInformation($"Attempting to subscribe to topic: {topic}");
        consumer.Subscribe(topic);

        CancellationTokenSource cts = new CancellationTokenSource();
        _logger.LogInformation($"Value of cts.Token -> : {cts.Token}");
        Console.CancelKeyPress += (_, e) =>
        {
            // Prevent the process from terminating.
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            _logger.LogInformation("Starting message consumption.");

            while (true)
            {
                var consumeResult = consumer.Consume(cts.Token);

                if (consumeResult != null && consumeResult.Message != null)
                {
                    _logger.LogInformation($"Consumed message: {consumeResult.Message.Value}");
                }
                else if (consumeResult == null)
                {
                    _logger.LogWarning("Consume returned null. Possible reasons:");
                }
            }
        }
        catch (KafkaException ex)
        {
            _logger.LogError($"Kafka exception occurred: {ex.Message}");
            _logger.LogError("Ensure the Kafka broker is running and reachable.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Message consumption was canceled.");
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Consumer closed.");
        }
    }
}