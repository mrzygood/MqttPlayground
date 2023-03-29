namespace MoreManagedClient.Connections;

public sealed record MqttConnectionConfig(string Url, int Port, string Login, string Password)
{
    public string GetAddress() => $"{Url}:{Port}";
}
