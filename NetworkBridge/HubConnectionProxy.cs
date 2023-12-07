using Enigma5.App.Common.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR.Client;

namespace NetworkBridge;

public class HubConnectionProxy
{
    private readonly IList<(HubConnection local, HubConnection remote)> _connections;

    private readonly Mutex _mutex;

    private long _attempts = 0;

    private long _failed = 0;

    public HubConnectionProxy(IList<(HubConnection inbound, HubConnection outbound)> connections)
    {
        _connections = connections;
        _mutex = new();

        foreach (var (local, remote) in _connections)
        {
            remote.ForwardCloseSignalTo(local);
            local.ForwardCloseSignalTo(remote);

            remote.ForwardMessageRoutingTo(local);
            local.ForwardMessageRoutingTo(remote);

            remote.ForwardBroadcastingTo(local);
            local.ForwardBroadcastingTo(remote);
        }
    }

    public void Start()
    {
        Parallel.ForEach(_connections, async connection =>
        {
            await connection.remote.StartAsync();
            await connection.local.StartAsync();
        });
    }

    public void StartAuthentication()
    {
        Parallel.ForEach(_connections, async connection =>
        {
            await connection.StartAuthentication(OnAuthenticationCompleted);
        });
    }

    public void StopAsync()
    {
        Parallel.ForEach(_connections, async connection =>
        {
            await connection.remote.StopAsync();
        });
    }

    private async Task OnAuthenticationCompleted(bool success)
    {
        _mutex.WaitOne();

        _attempts++;
        _failed += success ? 0 : 1;

        Console.WriteLine($"[+] {_attempts} / {_connections.Count} peers authenticated.");

        if (_attempts == _connections.Count)
        {
            Console.WriteLine("[+] Triggering broadcast.");
            await _connections.First().local.InvokeAsync(nameof(IHub.TriggerBroadcast));
        }

        _mutex.ReleaseMutex();
    }
}
