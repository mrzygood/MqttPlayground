using MoreManagedClient.Brokers;

namespace MoreManagedClient.Connections;

public interface IMqttConnectionPool
{
    bool IsConnectionExists(Guid brokerId);
    bool IsListenerExists(Guid brokerId, string topic);
    Task ConnectAsync(Broker broker);
    Task DisconnectAsync(Guid brokerId);
    Task AddListenersAsync(Guid brokerId, List<string> topics);
    Task RemoveListenersAsync(Guid brokerId, List<string> topics);
}
