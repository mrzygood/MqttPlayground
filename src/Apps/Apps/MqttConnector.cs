using System.Text;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;

namespace Apps;

public interface IMqttConnector
{
    Task ConnectAsync();
    Task DisconnectAsync();
    Task SubscribeAsync(string topic);
}

public sealed class MqttConnector : IMqttConnector
{
    private IManagedMqttClient? _client;
    private readonly MqttConnectionConfig _config;
    private readonly IDictionary<string, Func<Task>> _handlers;
    
    public MqttConnector(IOptions<MqttConnectionConfig> config)
    {
        _config = config.Value;
    }

    public async Task ConnectAsync()
    {
        var mqttFactory = new MqttFactory();

        _client = mqttFactory.CreateManagedMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_config.Url, _config.Port)
            .WithCredentials(_config.User, _config.Password)
            .Build();

        var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(mqttClientOptions)
            .Build();

        await _client.StartAsync(managedMqttClientOptions);

        _client.ConnectedAsync += _ =>
        {
            Console.WriteLine("Connected");
            return Task.CompletedTask;
        };

        _client.ApplicationMessageReceivedAsync += args =>
        {
            var messageJson = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);
            Console.WriteLine($"Received: {messageJson}");
            return Task.CompletedTask;
        };
    }

    public async Task DisconnectAsync()
    {
        if (_client is not null)
        {
            await _client.StopAsync();
        }
    }
    public async Task SubscribeAsync(string topic)
    {
        await _client.SubscribeAsync(topic);
    }

    // public async Task PublishAsync(string topic, object payload)
    // {
    //     var payloadAsString = JsonConvert.SerializeObject(payload);
    //     await _client.EnqueueAsync(topic, payloadAsString);
    // }
    //
    // public async Task SubscribeAsync(string topic, Func<Task> handler)
    // {
    //     _handlers[topic] = handler;
    //     await _client.SubscribeAsync(topic);
    // }
}
