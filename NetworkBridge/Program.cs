using Enigma5.App.Common.Extensions;
using NetworkBridge;

var configurationLoader = new ConfigurationLoader("appsettings.json");
var connections = HubConnectionsProxy.Create(configurationLoader);
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

    for (int i = 0; i < configurationLoader.Configuration.GetConnectionRetriesCount(); i++)
    {
        await Task.Delay(configurationLoader.Configuration.GetDelayBetweenConnectionRetries());
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
