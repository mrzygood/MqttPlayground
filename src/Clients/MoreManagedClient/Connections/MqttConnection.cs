using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace MoreManagedClient.Connections;

public sealed class MqttConnection
{
    private readonly IManagedMqttClient _client;
    private readonly ManagedMqttClientOptions _clientOptions;
    private readonly ILogger<MqttConnection>? _logger;

    private bool _connectionRequested;
    private int _reconnectionAttempts;
    private bool _disconnectionRequested;
    private DateTime? _disconnectedAt;
    
    private readonly List<string> _topics = new ();

    public bool IsStarted => _connectionRequested;
    public bool HasSubscriptions => _topics.Any();

    private string BrokerAddress => _client.Options.ClientOptions.ChannelOptions.ToString() ?? string.Empty;

    public MqttConnection(
        MqttConnectionConfig connectionConfig,
        Func<string, string, Task> messageHandler,
        ILogger<MqttConnection>? logger = null)
    {
        _logger = logger;
        
        var mqttFactory = new MqttFactory();
        _client = mqttFactory.CreateManagedMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithCleanSession()
            .WithTcpServer(connectionConfig.Url, connectionConfig.Port)
            .WithCredentials(connectionConfig.Login, connectionConfig.Password)
            .Build();

        _client.ApplicationMessageReceivedAsync += async messageArguments => await HandleMessage(messageArguments, messageHandler);
        _client.ConnectedAsync += HandleSuccessfulConnection;
        _client.ConnectingFailedAsync += HandleFailedConnection;
        _client.DisconnectedAsync += HandleDisconnection;
        
        _clientOptions = new ManagedMqttClientOptionsBuilder()
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
        _disconnectionRequested = false;
        
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

    private async Task HandleMessage(
        MqttApplicationMessageReceivedEventArgs arguments,
        Func<string, string, Task> handler)
    {
        var messageJson = Encoding.UTF8.GetString(arguments.ApplicationMessage.Payload);
        await handler(arguments.ApplicationMessage.Topic, messageJson);
    }

    private Task HandleSuccessfulConnection(MqttClientConnectedEventArgs arguments)
    {
        _disconnectedAt = null;
        _reconnectionAttempts = 0;
            
        _logger?.LogInformation(
            "Connected to MQTT broker with address '{mqttBrokerAddress}'",
            _client.Options.ClientOptions.ChannelOptions.ToString());
            
        return Task.CompletedTask;
    }

    private async Task HandleDisconnection(MqttClientDisconnectedEventArgs arguments)
    {
        _disconnectedAt ??= DateTime.UtcNow;

        if (_disconnectionRequested)
        {
            _logger?.LogDebug("Disconnected from MQTT broker with address '{brokerAddress}'", BrokerAddress);
            return;
        }

        if (_reconnectionAttempts > 0)
        {
            _logger?.LogInformation(
                "Reconnection to MQTT broker with address '{brokerAddress}' failed. " +
                "Attempt: {reconnectionAttemptNumber}",
                BrokerAddress,
                _reconnectionAttempts);
        }
        
        await UpdateReconnectionStrategyAsync();
    }

    private async Task HandleFailedConnection(ConnectingFailedEventArgs arguments)
    {
        if (_disconnectionRequested)
        {
            return;
        }
        
        _logger?.LogError(
            "Connection to MQTT broker with address '{mqttBrokerAddress}' failed. " +
            "Reason: {mqttConnectionFailedReason}",
            BrokerAddress,
            arguments.Exception.InnerException?.Message ?? arguments.Exception.Message);
        
        var invalidCredentialsCodeName = nameof(MqttClientConnectResultCode.BadUserNameOrPassword);
        if (arguments.Exception.Message.Contains(invalidCredentialsCodeName))
        {
            await _client.StopAsync();
        }
    }

    private async Task UpdateReconnectionStrategyAsync()
    {
        if (_disconnectedAt is not null)
        {
            var isMaxReconnectionDurationExceeded = (DateTime.UtcNow - _disconnectedAt.Value).TotalMinutes >= 10;
            if (isMaxReconnectionDurationExceeded)
            {
                await _client.StopAsync();
            }
        }
        
        var reconnectionFrequencyPower = _reconnectionAttempts;
        if (_reconnectionAttempts >= 8)
        {
            reconnectionFrequencyPower = 8;
        }

        _reconnectionAttempts++;
            
        var reconnectDelay = (int)Math.Pow(2, reconnectionFrequencyPower);
        _client.Options.AutoReconnectDelay = TimeSpan.FromSeconds(reconnectDelay);
    }
}
