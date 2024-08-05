using Enigma5.App.Common.Extensions;

namespace NetworkBridge;

public class Program
{
    private static readonly string _configFile = "appsettings.json";

    private static readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    private static readonly ConfigurationLoader _configurationLoader = new(_configFile);

    private static readonly HubConnectionsProxy _connections = HubConnectionsProxy.Create(_configurationLoader);

    public static async Task Main()
    {
        RegisterEvents();

        await StartAsync();
        Console.WriteLine("Connection successfully completed.");
        await BlockAsync();
    }

    private static void RegisterEvents()
    {
        _connections.OnAnyTargetClosed += OnConnectionClosedAsync;
        _connections.OnConnectionsReloaded += async () => await OnConnectionClosedAsync(null);
    }

    private static async Task StartAsync()
    {
        if (!await _connections.StartAsync())
        {
            throw new Exception("Some peers could not be connected.");
        }

        if (!await _connections.StartAuthenticationAsync())
        {
            throw new Exception("Authentication failed on some peers.");
        }

        if (!await _connections.TriggerBroadcast())
        {
            throw new Exception("Failed to trigger broadcast.");
        }
    }

    private static async Task OnConnectionClosedAsync(Exception? ex)
    {
        // TODO: log exception
        await _semaphoreSlim.WaitAsync();

        for (int i = 0; i < _configurationLoader.Configuration.GetConnectionRetriesCount(); i++)
        {
            await Task.Delay(_configurationLoader.Configuration.GetDelayBetweenConnectionRetries());
            try
            {
                await StartAsync();
                Console.WriteLine("Connections established");
                break;
            }
            catch (Exception)
            {
                // TODO: Log failed attempt
                Console.WriteLine("Failed attempt to reestablish connection.");
                continue;
            }
        }

        _semaphoreSlim.Release();
    }

    private static async Task BlockAsync()
    {
        while (true) await Task.Delay(int.MaxValue);
    }
}
