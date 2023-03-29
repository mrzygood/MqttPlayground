using MQTTnet.Adapter;
using RawClient.Brokers;
using RawClient.Dispatchers;

namespace RawClient.Connections;

public sealed class MqttConnectionPool : IMqttConnectionPool
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Dictionary<Guid, MqttConnection> _connections = new ();

    public MqttConnectionPool(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task ConnectAsync(Broker broker)
    {
        if (_connections.ContainsKey(broker.Id))
        {
            return;
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var connectionLogger = scope.ServiceProvider.GetRequiredService<ILogger<MqttConnection>>();
        var connectionDispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();

        var connectionString = new MqttConnectionConfig(broker.Url, broker.Port, broker.Login, broker.Password);
        var newConnection = new MqttConnection(
            connectionString,
            (topic, messageJson) => connectionDispatcher.DispatchAsync(topic, messageJson),
            connectionLogger);
        _connections[broker.Id] = newConnection;

        var (isConnected, resultStatus) = await newConnection.ConnectAsync();
    }

    public async Task DisconnectAsync(Guid brokerId)
    {
        var connectionKey = brokerId;
        if (_connections.ContainsKey(connectionKey) is false)
        {
            return;
        }

        var connection = _connections[connectionKey];
        if (connection.IsStarted)
        {
            await connection.DisconnectAsync();
        }

        _connections.Remove(connectionKey);
    }

    public async Task AddListenersAsync(Guid brokerId, List<string> topics)
    {
        var connectionKey = brokerId;
        if (_connections.ContainsKey(connectionKey) is false)
        {
            throw new Exception($"Broker {brokerId} not found");
        }

        var connection = _connections[connectionKey];
        if (connection.IsStarted is false)
        {
            await connection.ConnectAsync();
        }
        
        await connection.AddSubscription(topics);
    }

    public async Task RemoveListenersAsync(Guid brokerId, List<string> topics)
    {
        var connectionKey = brokerId;
        if (_connections.ContainsKey(connectionKey) is false)
        {
            throw new Exception($"Broker {brokerId} not found");
        }

        var connection = _connections[connectionKey];
        await connection.RemoveSubscription(topics);

        if (connection.IsStarted && connection.HasSubscriptions is false)
        {
            await DisconnectAsync(brokerId);
        }
    }

    public bool IsConnectionExists(Guid brokerId)
    {
        return _connections.ContainsKey(brokerId);
    }

    public bool IsListenerExists(Guid brokerId, string topic)
    {
        return _connections.ContainsKey(brokerId) && _connections[brokerId].HasSubscriber(topic);
    }
}
