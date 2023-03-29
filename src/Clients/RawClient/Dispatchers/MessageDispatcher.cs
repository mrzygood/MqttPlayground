namespace RawClient.Dispatchers;

public class MessageDispatcher : IMessageDispatcher
{
    private readonly ILogger<MessageDispatcher> _logger;

    public MessageDispatcher(ILogger<MessageDispatcher> logger)
    {
        _logger = logger;
    }

    public Task DispatchAsync(string topic, string messageAsJson)
    {
        _logger.LogDebug(
            "Message on topic: {topicName} dispatched. Content: {messageContent}",
            topic,
            messageAsJson);
        return Task.CompletedTask;
    }
}
