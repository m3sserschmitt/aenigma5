using Enigma5.App.Models;

namespace Enigma5.App.Common.Contracts.Hubs;

public interface IHub
{
    string? GenerateToken();

    Task<bool> Authenticate(AuthenticationRequest request);

    Signature? SignToken(string token);

    Task<bool> Broadcast(VertexBroadcast broadcastAdjacencyList);

    Task<bool> TriggerBroadcast();

    Task<bool> RouteMessage(string data);
}
