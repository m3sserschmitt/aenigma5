using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace NetworkBridge;

public static class HubConnectionExtensions
{
    private static Task InvokeAsync<T>(HubConnection target, string method, T data)
    {
        try
        {
            return target.InvokeAsync(method, data);
        }
        catch (Exception)
        {
            // TODO: Log this exception!
            return Task.CompletedTask;
        }
    }

    public static void Forward<T>(this ConnectionVector connectionVector, string method)
    where T : class
    {
        connectionVector.SourceOn<T>(method, async data => await connectionVector.InvokeTargetAsync(method, data));
        connectionVector.TargetOn<T>(method, async data => await connectionVector.InvokeSourceAsync(method, data));
    }

    public static void ForwardMessageRouting(this ConnectionVector connection)
    {
        connection.Forward<RoutingRequest>(nameof(IHub.RouteMessage));
    }

    public static void ForwardBroadcasts(this ConnectionVector connection)
    {
        connection.Forward<VertexBroadcastRequest>(nameof(IHub.Broadcast));
    }

    public static void ForwardCloseSignal(this ConnectionVector connection)
    {
        connection.SourceClosed += async _ => { try { await connection.StopTargetAsync(); } catch (Exception) { /* TODO: log exception */ } };
        connection.TargetClosed += async _ => { try { await connection.StopSourceAsync(); } catch (Exception) { /* TODO: log exception */ } };
    }

    public static async Task<bool> StartAsync(this IEnumerable<ConnectionVector> connections)
    {
        var results = await Task.WhenAll(connections.Select(async connection => await connection.StartAsync()));
        return results.All(result => result);
    }

    public static async Task<bool> StopAsync(this IEnumerable<ConnectionVector> connections)
    {
        var results = await Task.WhenAll(connections.Select(async connection => await connection.StopAsync()));
        return results.All(result => result);
    }

    public static async Task<bool> StartAuthenticationAsync(this IEnumerable<ConnectionVector> connections)
    {
        var results = await Task.WhenAll(connections.Select(async connection => await connection.StartAuthenticationAsync()));
        return results.All(result => result);
    }
}
