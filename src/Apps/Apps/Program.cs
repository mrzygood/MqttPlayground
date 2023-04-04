using Apps;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MqttConnectionConfig>(
    builder.Configuration.GetSection("Mqtt"));
builder.Services.AddSingleton<IMqttConnector, MqttConnector>();
builder.Services.AddHostedService<MqttConnectionInitialized>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
