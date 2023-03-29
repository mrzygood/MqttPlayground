namespace MoreManagedClient.Connections;

public enum MqttConnectionStatus
{
    Connected = 1,
    InvalidCredentials = 2,
    NetworkIssue = 3,
    Unspecified = 4,
}
