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

    public static Task StartAuthentication(this (HubConnection first, HubConnection second) pair, Func<bool, Task> onCompleted)
    => pair.StartAuthentication(true, onCompleted);

    private static Task StartAuthentication(
        this (HubConnection first, HubConnection second) pair,
        bool authenticateInReverse,
        Func<bool, Task> onCompleted)
    {
        return pair.second.InvokeAsync<string?>(nameof(IHub.GenerateToken))
        .ContinueWith(async response =>
        {
            var token = await response ?? throw new Exception();

            await pair.first.InvokeAsync<Signature?>(nameof(IHub.SignToken), token)
            .ContinueWith(async response =>
            {
                var signature = await response ?? throw new Exception();

                await pair.second.InvokeAsync<bool>(nameof(IHub.Authenticate), new AuthenticationRequest
                {
                    Signature = signature.SignedData,
                    PublicKey = signature.PublicKey,
                    UpdateNetworkGraph = !authenticateInReverse,
                    SyncMessagesOnSuccess = false
                })
                .ContinueWith(async response =>
                {
                    var success = await response;
                    if (authenticateInReverse && success)
                    {
                        (HubConnection, HubConnection) reversed = (pair.second, pair.first);

                        await reversed.StartAuthentication(false, onCompleted);
                    }
                    else
                    {
                        await onCompleted(success);
                    }
                });
            });
        });
    }

    public static void ForwardCloseSignalTo(this HubConnection connection, HubConnection other)
    {
        connection.Closed += async _ => { await other.StopAsync(); };
    }
}
