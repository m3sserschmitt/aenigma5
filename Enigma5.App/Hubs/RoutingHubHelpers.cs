/*
    Aenigma - Federal messaging system
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Reflection;
using Enigma5.App.Attributes;
using Enigma5.App.Models;
using Enigma5.App.Models.HubInvocation;
using Enigma5.App.Resources.Commands;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Enigma5.App.Hubs;

public partial class RoutingHub
{
    protected async Task<bool> SendAsync(string connectionId, string method, object? arg1)
    {
        try
        {
            await Clients.Client(connectionId).SendAsync(method, arg1);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error encountered while calling {{{nameof(HubInvocationContext.HubMethodName)}}} for {{{nameof(Context.ConnectionId)}}}.",
            nameof(SendAsync),
            Context.ConnectionId);
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

            await Clients.Client(connectionId).SendAsync(routingMethod.Name, new RoutingRequest(Convert.ToBase64String(data)));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error encountered while calling {{{nameof(HubInvocationContext.HubMethodName)}}} for {{{nameof(Context.ConnectionId)}}}.",
            nameof(RouteMessage),
            Context.ConnectionId);
            return false;
        }
    }

    private async Task<VertexBroadcastRequest?> AddNewAdjacencies(List<string> addresses)
    => (await _commandRouter.Send(new UpdateLocalAdjacencyCommand(addresses, true))).Value;


    private async Task<VertexBroadcastRequest?> RemoveAdjacencies(List<string> addresses)
    => (await _commandRouter.Send(new UpdateLocalAdjacencyCommand(addresses, false))).Value;

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

    private static InvocationResult<T> Ok<T>(T response) => new SuccessResult<T>(response);

    private static Task<InvocationResult<T>> OkAsync<T>(T response) => Task.FromResult(Ok(response));

    private static InvocationResult<T> Error<T>(T? response, string error) => ErrorResult<T>.Create(response, error);

    private static Task<InvocationResult<T>> ErrorAsync<T>(T? response, string error) => Task.FromResult(Error(response, error));

    private static InvocationResult<T> Error<T>(string error) => Error<T>(default, error);

    private static Task<InvocationResult<T>> ErrorAsync<T>(string error)
    => Task.FromResult(Error<T>(error));
}
