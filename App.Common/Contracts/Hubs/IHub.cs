using Enigma5.App.Models;

namespace Enigma5.App.Common.Contracts.Hubs;

public interface IHub
{
    Task GenerateToken();

    Task<bool> Authenticate(AuthenticationRequest request);

    Task SignToken(string token);

    Task Broadcast(BroadcastAdjacencyList broadcastAdjacencyList);

    Task TriggerBroadcast();

    Task RouteMessage(string data);
}
