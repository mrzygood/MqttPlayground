using MoreManagedClient.Brokers;
using MoreManagedClient.Connections;
using MoreManagedClient.Dispatchers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IBrokerRepository, InMemoryBrokerRepository>();
builder.Services.AddSingleton<IMqttConnectionPool, MqttConnectionPool>();
builder.Services.AddTransient<IMessageDispatcher, MessageDispatcher>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/add-broker", async (
    IBrokerRepository brokerRepository,
    IMqttConnectionPool connectionPool,
    string login, string password, string? brokerUrl, int? port) =>
{
    var broker = new Broker(Guid.NewGuid(), brokerUrl ?? "localhost", port ?? 1884, login, password);
    await brokerRepository.AddAsync(broker);
    await connectionPool.ConnectAsync(broker);
    return Results.Ok(broker.Id);
});

app.MapGet("/update-broker", async (
    IBrokerRepository brokerRepository,
    IMqttConnectionPool connectionPool,
    Guid id, string? login, string? password, string? brokerUrl, int? port) =>
{
    var broker = await brokerRepository.GetAsync(id);
    if (broker is null)
    {
        throw new Exception($"Broker {id} not found");
    }
    
    if (login is not null) broker.Login = login; 
    if (password is not null) broker.Password = password; 
    if (brokerUrl is not null) broker.Url = brokerUrl; 
    if (port is not null) broker.Port = (int)port;
    
    await connectionPool.DisconnectAsync(id);
    await connectionPool.ConnectAsync(broker);
});

app.MapGet("/subscribe-topic", async (
    IBrokerRepository brokerRepository,
    IMqttConnectionPool connectionPool,
    Guid brokerId, string topic) =>
{
    var broker = await brokerRepository.GetAsync(brokerId);
    if (broker is null)
    {
        throw new Exception($"Broker {brokerId} not found");
    }

    await connectionPool.AddListenersAsync(brokerId, new List<string>() { topic });
});

app.Run();
