namespace Enigma5.App.Contracts;

public interface IConnectionsMapper
{
    public bool TryAdd(string address, string connectionId);

    public bool Remove(string connectionId);

    public bool TryGetConnectionId(string address, out string? connectionId);
}
