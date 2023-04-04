namespace Apps;

public sealed class MqttConnectionConfig
{
    public string Url { get; set; }
    public int Port { get; set; }
    public string User { get; set; }
    public string Password { get; init; }
}
