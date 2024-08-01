using System.Reflection;
using Enigma5.App.Attributes;
using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.Crypto;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs;

public partial class RoutingHub
{
    protected async Task RespondAsync(string method, object? arg1)
    => await Clients.Client(Context.ConnectionId).SendAsync(method, arg1);

    protected async Task<bool> SendAsync(string connectionId, string method, object? arg1)
    {
        try
        {
            await Clients.Client(connectionId).SendAsync(method, arg1);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    protected async Task<bool> RouteMessage(string connectionId, byte[] data)
    {
        try
        {
            var routingMethod = typeof(RoutingHub).GetMethods()
                    .Where(m => m.GetCustomAttribute<OnionRoutingAttribute>() != null)
                    .SingleOrDefault()
                    ?? throw new Exception($"Type {nameof(RoutingHub)} should contain exactly one method with {nameof(OnionRoutingAttribute)}.");

            await Clients.Client(connectionId).SendAsync(routingMethod.Name, Convert.ToBase64String(data));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<(Vertex localVertex, VertexBroadcastRequest? broadcast)> AddNewAdjacency(string address)
    => await _commandRouter.Send(new UpdateLocalAdjacencyCommand(address, true));


    private async Task<(Vertex localVertex, VertexBroadcastRequest? broadcast)> RemoveAdjacency(string address)
    => await _commandRouter.Send(new UpdateLocalAdjacencyCommand(address, false));

    private IEnumerable<Task<bool>> GenerateBroadcastTask(IEnumerable<VertexBroadcastRequest> adjacencyLists)
    {
        foreach (var address in _networkGraph.NeighboringAddresses)
        {
            if (_sessionManager.TryGetConnectionId(address, out string? connectionId))
            {
                foreach (var adjacencyList in adjacencyLists)
                {
                    yield return SendAsync(connectionId!, nameof(Broadcast), adjacencyList);
                }
            }
        }
    }

    private async Task<bool> SendBroadcast(IEnumerable<VertexBroadcastRequest> adjacencyLists)
    {
        return (await Task.WhenAll(GenerateBroadcastTask(adjacencyLists))).All(success => success);
    }

    private async Task<bool> SendBroadcast(VertexBroadcastRequest adjacencyLists)
    => await SendBroadcast([adjacencyLists]);

    private static InvocationResult<T> Ok<T>(T response) => new(response);

    private static Task<InvocationResult<T>> OkAsync<T>(T response) => Task.FromResult(Ok(response));

    private static InvocationResult<T> Error<T>(T? response, string error) => new(response, [new(error, [])]);

    private static Task<InvocationResult<T>> ErrorAsync<T>(T? response, string error) => Task.FromResult(Error(response, error));

    private static InvocationResult<T> Error<T>(string error) => new(default, [new(error, [])]);

    private static Task<InvocationResult<T>> ErrorAsync<T>(string error)
    => Task.FromResult(Error<T>(error));
}
