using Enigma5.App.Common.Utils;

namespace Enigma5.App.Hubs.Sessions;

public class ConnectionsMapper
{
    private readonly object _locker = new();

    private readonly Dictionary<string, string> _connections = [];

    public bool TryAdd(string address, string connectionId)
    => ThreadSafeExecution.Execute(() => _connections.TryAdd(address, connectionId), false, _locker);

    public bool Remove(string connectionId, out string? address)
    => ThreadSafeExecution.Execute(
        (out string? addr) =>
        {
            addr = null;
            
            foreach (var pair in _connections)
            {
                if (pair.Value == connectionId)
                {
                    addr = pair.Key;
                    break;
                }
            }

            if (addr == null || !_connections.Remove(addr, out var _))
            {
                return false;
            }

            return true;
        },
        false,
        out address,
        _locker
    );

    public bool TryGetConnectionId(string address, out string? connectionId)
    => ThreadSafeExecution.Execute(
        (out string? connId) => _connections.TryGetValue(address, out connId),
        false,
        out connectionId,
        _locker
    );

    public bool TryGetAddress(string connectionId, out string? address)
    => ThreadSafeExecution.Execute(
        (out string? addr) =>
        {
            try
            {
                var item = _connections.First(item => item.Value == connectionId);
                addr = item.Key;
                return true;
            }
            catch
            {
                addr = null;
                return false;
            }
        },
        false,
        out address,
        _locker
    );
}
