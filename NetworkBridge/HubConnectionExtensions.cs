using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace NetworkBridge;

public static class HubConnectionExtensions
{
    public static void ForwardTo<T>(this HubConnection connection, HubConnection other, string method)
    where T : class
    {
        connection.On<T>(method, data =>
        {
            other.InvokeAsync(method, data);
        });
    }

    public static void ForwardMessageRoutingTo(this HubConnection connection, HubConnection other)
    => connection.ForwardTo<string>(other, nameof(IHub.RouteMessage));

    public static void ForwardBroadcastingTo(this HubConnection connection, HubConnection other)
    => connection.ForwardTo<BroadcastAdjacencyList>(other, nameof(IHub.Broadcast));

    public static void ForwardTokensForSigningTo(this HubConnection connection, HubConnection other)
    {
        connection.On<string>(nameof(IHub.GenerateToken), token =>
        {
            other.InvokeAsync(nameof(IHub.SignToken), token);
        });
    }

    public static void ForwardAuthenticationTo(this HubConnection connection, HubConnection other, bool updateNetworkGraph, bool broadcast, bool syncMessagesOnSuccess = false)
    {
        connection.On<AuthenticationRequest>(nameof(IHub.Authenticate), data =>
        {
            other.InvokeAsync(nameof(IHub.Authenticate), new AuthenticationRequest {
                PublicKey = data.PublicKey,
                Signature = data.Signature,
                SyncMessagesOnSuccess = syncMessagesOnSuccess,
                UpdateNetworkGraph = updateNetworkGraph,
            });
        });
    }

    public static void ForwardCloseSignalTo(this HubConnection connection, HubConnection other)
    {
        connection.Closed += async _ => { await other.StopAsync(); };
    }
}
