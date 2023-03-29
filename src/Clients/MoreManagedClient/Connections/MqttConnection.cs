using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace MoreManagedClient.Connections;

public sealed class MqttConnection
{
    private readonly IManagedMqttClient _client;
    private readonly ManagedMqttClientOptions _clientOptions;

    private readonly List<string> _topics = new ();
    
    private bool _connectionRequested;
    private bool _disconnectionRequested;

    public bool IsStarted => _connectionRequested;
    public bool HasSubscriptions => _topics.Any();

    public MqttConnection(
        MqttConnectionConfig connectionConfig,
        Func<string, string, Task> messageHandler,
        ILogger<MqttConnection>? logger = null)
    {
        var mqttFactory = new MqttFactory();

        _client = mqttFactory.CreateManagedMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(connectionConfig.Url, connectionConfig.Port)
            .WithCredentials(connectionConfig.Login, connectionConfig.Password)
            .Build();

        _client.ApplicationMessageReceivedAsync += async messageArguments =>
        {
            var messageJson = Encoding.UTF8.GetString(messageArguments.ApplicationMessage.Payload);
            await messageHandler(messageArguments.ApplicationMessage.Topic, messageJson);
        };

        _client.ConnectedAsync += _ =>
        {
            logger?.LogInformation(
                "Connected to MQTT broker with address '{mqttBrokerAddress}'",
                connectionConfig.GetAddress());
            return Task.CompletedTask;
        };

        _client.ConnectingFailedAsync += connectionFailedEvent =>
        {
            logger?.LogError(
                "Connection to MQTT broker with address '{mqttBrokerAddress}' failed. Reason {mqttConnectionFailedReason}",
                connectionConfig.GetAddress(),
                connectionFailedEvent.Exception.Message);
            return Task.CompletedTask;
        };

        _client.DisconnectedAsync += disconnectedEvent =>
        {
            logger?.LogError(
                "Disconnected from MQTT broker with address '{brokerAddress}' failed. Reason {mqttDisconnectedReason}",
                connectionConfig.GetAddress(),
                disconnectedEvent.ReasonString);
            return Task.CompletedTask;
        };

        _clientOptions = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(mqttClientOptions)
            .Build();
    }

    public bool HasSubscriber(string topic)
    {
        return _topics.Contains(topic);
    }

    public async Task AddSubscription(List<string> topics)
    {
        foreach (var topic in topics)
        {
            await AddSubscription(topic);
        }
    }

    public async Task RemoveSubscription(List<string> topics)
    {
        foreach (var topic in topics)
        {
            await RemoveSubscription(topic);
        }
    }

    public async Task ConnectAsync()
    {
        _connectionRequested = true;
        
        if (_client.IsConnected)
        {
            return;
        }

        await _client.StartAsync(_clientOptions);
    }

    public async Task DisconnectAsync()
    {
        if (_connectionRequested is false || _disconnectionRequested)
        {
            return;
        }

        _disconnectionRequested = true;
        await _client.StopAsync();
    }

    private async Task AddSubscription(string topic)
    {
        if (_topics.Contains(topic))
        {
            return;
        }

        await _client.SubscribeAsync(topic);
        _topics.Add(topic);
    }

    private async Task RemoveSubscription(string topic)
    {
        if (_topics.Contains(topic) is false)
        {
            return;
        }

        await _client.UnsubscribeAsync(topic);
        _topics.Remove(topic);
    }
}
