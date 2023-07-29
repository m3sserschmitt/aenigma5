namespace Enigma5.App.Hubs.Sessions;

public class ConnectionsMapper
{
    private readonly Dictionary<string, string> connections = new();

    public bool TryAdd(string address, string connectionId)
    {
        return connections.TryAdd(address, connectionId);
    }

    public bool Remove(string connectionId)
    {
        string? address = null;

        foreach(var pair in connections)
        {
            if(pair.Value == connectionId)
            {
                address = pair.Key;
                break;
            }
        }

        return address != null && connections.Remove(address);
    }

    public bool TryGetConnectionId(string address, out string? connectionId)
    {
        return connections.TryGetValue(address, out connectionId);
    }
}
