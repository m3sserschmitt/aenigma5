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
    
    private async Task<(Vertex localVertex, VertexBroadcast? broadcast)> AddNewAdjacency(string publicKey)
    {
        var command = new UpdateLocalAdjacencyCommand(CertificateHelper.GetHexAddressFromPublicKey(publicKey), true);

        return await _commandRouter.Send(command);
    }

    private async Task<(Vertex localVertex, VertexBroadcast? broadcast)> RemoveAdjacency(string address)
    {
        var command = new UpdateLocalAdjacencyCommand(address, false);

        return await _commandRouter.Send(command);
    }

    private IEnumerable<Task<bool>> GenerateBroadcastTask(IEnumerable<VertexBroadcast> adjacencyLists)
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

    private async Task<bool> SendBroadcast(IEnumerable<VertexBroadcast> adjacencyLists)
    {
        return (await Task.WhenAll(GenerateBroadcastTask(adjacencyLists))).All(success => success);
    }

    private async Task<bool> SendBroadcast(VertexBroadcast adjacencyLists)
    => await SendBroadcast([adjacencyLists]);
}
