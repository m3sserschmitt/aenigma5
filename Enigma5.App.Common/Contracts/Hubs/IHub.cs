using Enigma5.App.Models;

namespace Enigma5.App.Common.Contracts.Hubs;

public interface IHub
{
    Task<InvocationResult<string>> GenerateToken();

    Task<InvocationResult<bool>> Authenticate(AuthenticationRequest request);

    Task<InvocationResult<Signature>> SignToken(SignatureRequest request);

    Task<InvocationResult<bool>> Broadcast(VertexBroadcastRequest request);

    Task<InvocationResult<bool>> TriggerBroadcast(TriggerBroadcastRequest request);

    Task<InvocationResult<bool>> RouteMessage(RoutingRequest request);
}
