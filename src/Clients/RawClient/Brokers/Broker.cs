namespace RawClient.Brokers;

public class Broker
{
    public Broker(Guid brokerId, string url, int port, string login, string password)
    {
        Id = brokerId;
        Port = port;
        Url = url;
        Login = login;
        Password = password;
    }

    public Guid Id { get; private set; }
    public int Port { get; set; }
    public string Url { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
}
