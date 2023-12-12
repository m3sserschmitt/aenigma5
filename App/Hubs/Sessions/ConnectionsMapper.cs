namespace Enigma5.App.Hubs.Sessions;

public class ConnectionsMapper
{
    private readonly Dictionary<string, string> connections = new();

    public bool TryAdd(string address, string connectionId)
    {
        return connections.TryAdd(address, connectionId);
    }

    public bool Remove(string connectionId, out string? address)
    {
        address = null;

        foreach (var pair in connections)
        {
            if (pair.Value == connectionId)
            {
                address = pair.Key;
                break;
            }
        }

        if (address == null || !connections.Remove(address))
        {
            return false;
        }

        return true;
    }

    public bool TryGetConnectionId(string address, out string? connectionId)
    {
        return connections.TryGetValue(address, out connectionId);
    }

    public bool TryGetAddress(string connectionId, out string? address)
    {
        try
        {
            var item = connections.First(item => item.Value == connectionId);
            address = item.Key;
            return true;
        }
        catch
        {
            address = null;
        }
        return false;
    }
}
