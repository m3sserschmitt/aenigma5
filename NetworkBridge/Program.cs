using Enigma5.App.Common.Extensions;
using Microsoft.Extensions.Configuration;
using NetworkBridge;

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

var urls = configuration.GetPeers() ?? throw new Exception("Peers section not provided into configuration.");

var hubConnectionFactory = new HubConnectionFactory(configuration);
var connections = hubConnectionFactory.CreateConnectionsProxy(urls);
connections.OnAnyTargetClosed += OnConnectionClosed;

async Task Start()
{
    if (!await connections.StartAsync())
    {
        throw new Exception("Some peers could not be connected.");
    }

    if (!await connections.StartAuthenticationAsync())
    {
        throw new Exception("Authentication failed on some peers.");
    }

    if (!await connections.TriggerBroadcast())
    {
        throw new Exception("Failed to trigger broadcast.");
    }
}

async Task OnConnectionClosed(Exception? ex)
{
    // TODO: log exception

    for (int i = 0; i < configuration.GetConnectionRetriesCount(); i++)
    {
        await Task.Delay(configuration.GetDelayBetweenConnectionRetries());
        try
        {
            await Start();
            Console.WriteLine("Connections reestablished.");
            break;
        }
        catch (Exception)
        {
            // TODO: Log failed attempt
            Console.WriteLine("Failed attempt to reestablish connection.");
            continue;
        }
    }
};

await Start();

Console.WriteLine("Connection successfully completed.");

while(true) Task.Delay(int.MaxValue).Wait();
