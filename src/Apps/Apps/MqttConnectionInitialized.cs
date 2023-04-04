namespace Apps;

public sealed class MqttConnectionInitialized : IHostedService
{
    private readonly IMqttConnector _mqttConnector;

    public MqttConnectionInitialized(IMqttConnector mqttConnector)
    {
        _mqttConnector = mqttConnector;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _mqttConnector.ConnectAsync();
        await _mqttConnector.SubscribeAsync("test");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _mqttConnector.DisconnectAsync();
    }
}
