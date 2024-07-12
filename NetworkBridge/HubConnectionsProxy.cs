using Enigma5.App.Common.Contracts.Hubs;

namespace NetworkBridge;

public class HubConnectionsProxy
{
    private readonly List<ConnectionVector> _connections;

    public event Func<Exception?, Task> OnAnyTargetClosed
    {
        add
        {
            _connections.ForEach(connection => connection.TargetClosed += value);
        }
        remove
        {
            _connections.ForEach(connection => connection.TargetClosed -= value);
        }
    }

    public event Func<Exception?, Task> OnAnySourceClosed
    {
        add
        {
            _connections.ForEach(connection => connection.SourceClosed += value);
        }
        remove
        {
            _connections.ForEach(connection => connection.SourceClosed -= value);
        }
    }

    public HubConnectionsProxy(List<ConnectionVector> connections)
    {
        _connections = connections;

        foreach (var connection in _connections)
        {
            connection.ForwardCloseSignal();
            connection.ForwardMessageRouting();
            connection.ForwardBroadcasts();
        }
    }

    public Task<bool> StartAsync() => _connections.StartAsync();

    public Task<bool> StartAuthenticationAsync() => _connections.StartAuthenticationAsync();

    public Task<bool> StopAsync() => _connections.StopAsync();

    public async Task<bool> TriggerBroadcast()
    {
        try
        {
            await _connections.First().InvokeSourceAsync(nameof(IHub.TriggerBroadcast));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
