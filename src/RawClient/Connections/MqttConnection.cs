using System.Net.Sockets;
using System.Text;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using Timer = System.Timers.Timer;

namespace RawClient.Connections;

public sealed class MqttConnection : IDisposable
{
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _clientOptions;

    private readonly List<string> _topics = new ();
    
    private bool _connectionRequested;
    private bool _disconnectionRequested;
    private int _failedConnectionsAttempts;

    private bool _reconnectingEnabled;
    private Timer? _reconnectTimer;

    private readonly ILogger<MqttConnection>? _logger;

    public bool IsStarted => _connectionRequested;
    public bool HasSubscriptions => _topics.Any();

    public MqttConnection(
        MqttConnectionConfig connectionConfig,
        Func<string, string, Task> messageHandler,
        ILogger<MqttConnection>? logger = null)
    {
        _logger = logger;
        
        var mqttFactory = new MqttFactory();

        _client = mqttFactory.CreateMqttClient();

        _client.ApplicationMessageReceivedAsync += async messageArguments =>
        {
            var messageJson = Encoding.UTF8.GetString(messageArguments.ApplicationMessage.Payload);
            await messageHandler(messageArguments.ApplicationMessage.Topic, messageJson);
        };

        _client.ConnectedAsync += _ =>
        {
            _reconnectingEnabled = false;
            _failedConnectionsAttempts = 0;
            
            logger?.LogInformation(
                "Connected to MQTT broker with address '{mqttBrokerAddress}'",
                connectionConfig.GetAddress());
            return Task.CompletedTask;
        };

        _client.DisconnectedAsync += disconnectedEvent =>
        {
            if (_reconnectingEnabled is false)
            {
                if (_disconnectionRequested || disconnectedEvent.ClientWasConnected is false)
                {
                    return Task.CompletedTask;
                }
            }
            
            if (_failedConnectionsAttempts < 3)
            {
                _reconnectingEnabled = true;
                EnqueueReconnect(TimeSpan.FromSeconds(_failedConnectionsAttempts == 0 ? 1 : _failedConnectionsAttempts * 3));
                return Task.CompletedTask;
            }
            
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
            _reconnectingEnabled = false;
            
            logger?.LogError(
                "Disconnected from MQTT broker with address '{brokerAddress}'. Reason {mqttDisconnectedReason}",
                connectionConfig.GetAddress(),
                disconnectedEvent.ReasonString);
            return Task.CompletedTask;
        };

        _clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(connectionConfig.Url, connectionConfig.Port)
            .WithCredentials(connectionConfig.Login, connectionConfig.Password)
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

    public async Task<(bool ConnectedSuccesfully, MqttConnectionStatus status)> ConnectAsync()
    {
        _connectionRequested = true;
        
        if (_client.IsConnected)
        {
            return (true, MqttConnectionStatus.Connected);
        }

        try
        {
            await _client.ConnectAsync(_clientOptions, CancellationToken.None);
        }
        catch (MqttConnectingFailedException exception)
        {
            if (exception.Message.Contains("BadUserNameOrPassword"))
            {
                return (false, MqttConnectionStatus.InvalidCredentials);
            }

            return (false, MqttConnectionStatus.Unspecified);
        }
        catch (MqttCommunicationException exception)
        {
            if (exception.InnerException is SocketException)
            {
                return (false, MqttConnectionStatus.NetworkIssue);
            }
            
            return (false, MqttConnectionStatus.Unspecified);
        }
        
        return (true, MqttConnectionStatus.Connected);
    }

    public async Task DisconnectAsync()
    {
        if (_connectionRequested is false || _disconnectionRequested)
        {
            return;
        }

        _disconnectionRequested = true;
        await _client.DisconnectAsync();
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

    private void EnqueueReconnect(TimeSpan nextAttempt)
    {
        _reconnectTimer = new Timer(nextAttempt.TotalMilliseconds);
        _reconnectTimer.Elapsed += async (_, _) =>
        {
            var attempt = _failedConnectionsAttempts + 1;
            try
            {
                _failedConnectionsAttempts++;
                await _client.ReconnectAsync();
            }
            catch (Exception)
            {
                _logger?.LogWarning($"Reconnection no. {attempt} failed");
            }
        };
        _reconnectTimer.AutoReset = false;
        _reconnectTimer.Enabled = true;
    }

    public void Dispose()
    {
        _reconnectTimer?.Dispose();
    }
}
