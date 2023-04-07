namespace RawClient.Dispatchers;

public interface IMessageDispatcher
{
    Task DispatchAsync(string topic, string messageAsJson);
}
