using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Models;
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

            remote.ForwardTokensForSigningTo(local);
            local.ForwardTokensForSigningTo(remote);

            remote.On<AuthenticationRequest>(nameof(IHub.Authenticate), data =>
            {
                local.InvokeAsync<bool>(nameof(IHub.Authenticate), new AuthenticationRequest
                {
                    PublicKey = data.PublicKey,
                    Signature = data.Signature,
                    SyncMessagesOnSuccess = false,
                    UpdateNetworkGraph = true,
                }).ContinueWith(async status =>
                {
                    _mutex.WaitOne();

                    _attempts++;
                    _failed += await status ? 0 : 1;

                    Console.WriteLine($"[+] {_attempts - _failed} / {_connections.Count} remotes authenticated to local.");

                    if (_attempts == _connections.Count - _failed)
                    {
                        Console.WriteLine("[+] Triggering broadcast.");
                        await _connections.First().local.InvokeAsync(nameof(IHub.TriggerBroadcast));
                    }

                    _mutex.ReleaseMutex();
                });
            });

            local.On<AuthenticationRequest>(nameof(IHub.Authenticate), data =>
            {
                remote.InvokeAsync<bool>(nameof(IHub.Authenticate), new AuthenticationRequest
                {
                    PublicKey = data.PublicKey,
                    Signature = data.Signature,
                    SyncMessagesOnSuccess = false,
                    UpdateNetworkGraph = false,
                }).ContinueWith(async success =>
                {
                    var ok = await success;

                    _mutex.WaitOne();

                    _failed += ok ? 0 : 1;

                    _mutex.ReleaseMutex();

                    if (ok)
                    {
                        await local.InvokeAsync(nameof(IHub.GenerateToken));
                    }
                });
            });
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
            await connection.remote.InvokeAsync(nameof(IHub.GenerateToken));
        });
    }

    public void StopAsync()
    {
        Parallel.ForEach(_connections, async connection =>
        {
            await connection.remote.StopAsync();
        });
    }
}
