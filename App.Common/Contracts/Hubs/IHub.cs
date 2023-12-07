using Enigma5.App.Models;

namespace Enigma5.App.Common.Contracts.Hubs;

public interface IHub
{
    string? GenerateToken();

    Task<bool> Authenticate(AuthenticationRequest request);

    Signature? SignToken(string token);

    Task Broadcast(BroadcastAdjacencyList broadcastAdjacencyList);

    Task TriggerBroadcast();

    Task RouteMessage(string data);
}
