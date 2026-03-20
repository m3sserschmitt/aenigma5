/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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
using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Models.HubInvocation;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Handlers;
using Enigma5.App.Resources.Queries;
using Enigma5.Crypto;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs;

public partial class RoutingHub
{
    protected async Task<bool> IsLocalAddress(string publicKey)
    => await _certificateManager.GetAddressAsync() == CertificateHelper.GetHexAddressFromPublicKey(publicKey);

    protected async Task<bool> Authenticate(string publicKey, string signature)
    => await _sessionManager.AuthenticateAsync(
            Context.ConnectionId,
            publicKey!,
            signature!,
            await IsLocalAddress(publicKey!) ? Context.Items[Common.Constants.XImpersonateServiceHeaderKey] as string : null);

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

    protected async Task<bool> RouteMessage(string connectionId, byte[] data, string? uuid)
    {
        try
        {
            var routingMethod = typeof(RoutingHub).GetMethods()
                    .SingleOrDefault(m => m.GetCustomAttribute<OnionRoutingAttribute>() != null)
                    ?? throw new Exception($"Type {nameof(RoutingHub)} should contain exactly one method with {nameof(OnionRoutingAttribute)}.");

            await Clients.Client(connectionId).SendAsync(routingMethod.Name, new RoutingRequestDto([Convert.ToBase64String(data)], uuid));
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

    private async Task<VertexBroadcastRequestDto?> AddNewAdjacencies(List<string> addresses)
    => (await _commandRouter.Send(new UpdateLocalAdjacencyCommand(addresses, true))).Value;


    private async Task<VertexBroadcastRequestDto?> RemoveAdjacencies(List<string> addresses)
    => (await _commandRouter.Send(new UpdateLocalAdjacencyCommand(addresses, false))).Value;

    private async Task<IEnumerable<Task<bool>>> GenerateBroadcastTask(IEnumerable<VertexBroadcastRequestDto> adjacencyLists)
    {
        var tasks = new List<Task<bool>>();
        var result = await _commandRouter.Send(new GetNeighborAddressesQuery());

        if (!result.IsSuccessNotNullResultValue())
        {
            return tasks;
        }

        foreach (var address in result.Value ?? [])
        {
            var connectionId = await _sessionManager.TryGetConnectionIdAsync(address);
            if (connectionId != null)
            {
                foreach (var adjacencyList in adjacencyLists)
                {
                    tasks.Add(SendAsync(connectionId!, nameof(Broadcast), adjacencyList));
                }
            }
        }
        return tasks;
    }

    private async Task<bool> SendBroadcast(IEnumerable<VertexBroadcastRequestDto> adjacencyLists)
    => (await Task.WhenAll(await GenerateBroadcastTask(adjacencyLists))).All(success => success);

    private async Task<CommandResult<PendingMessage>> CreatePendingMessage()
    {
        var isNeighbor = (await GetNeighborAddressesAsync()).Contains(Next!);
        if (Content != null && !isNeighbor)
        {
            _logger.LogDebug($"Saving onion for connectionId {{{nameof(Context.ConnectionId)}}}.", Context.ConnectionId);
            var encodedContent = Convert.ToBase64String(Content);
            if (encodedContent is null)
            {
                _logger.LogError($"Could not base64 onion content for connectionId {{{nameof(Context.ConnectionId)}}}", Context.ConnectionId);
                return CommandResult.CreateResultFailure<PendingMessage>();
            }
            return await _commandRouter.Send(new CreatePendingMessageCommand(Next!, encodedContent));
        }
        _logger.LogDebug($"Could not save pending message for connectionId {{{nameof(Context.ConnectionId)}}} because the content is null.", Context.ConnectionId);
        return CommandResult.CreateResultFailure<PendingMessage>();
    }

    private async Task<HashSet<string>> GetNeighborAddressesAsync()
    {
        var result = await _commandRouter.Send(new GetNeighborAddressesQuery());
        if (!result.IsSuccessNotNullResultValue())
        {
            return [];
        }
        return result.Value ?? [];
    }

    private async Task<List<PendingMessageDto>> GetPendingMessagesAsync(string address)
    {
        var result = await _commandRouter.Send(new GetPendingMessagesByDestinationQuery(address));
        if (!result.IsSuccessNotNullResultValue())
        {
            return [];
        }
        return result.Value ?? [];
    }

    private async Task SyncPendingMessages()
    {
        foreach (var address in await GetNeighborAddressesAsync())
        {
            var pendingMessages = await GetPendingMessagesAsync(address);
            if (pendingMessages.Count == 0)
            {
                continue;
            }

            var connectionId = await _sessionManager.TryGetConnectionIdAsync(address);
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                continue;
            }

            foreach (var item in pendingMessages)
            {
                if (string.IsNullOrWhiteSpace(item.Content))
                {
                    continue;
                }
                try
                {
                    await RouteMessage(connectionId, Convert.FromBase64String(item.Content), item.Uuid);
                }
                catch (Exception)
                {
                    continue;
                }
            }
            await _commandRouter.Send(new MarkMessagesAsDeliveredCommand(address));
        }
    }

    private async Task<bool> SendBroadcast(VertexBroadcastRequestDto adjacencyLists)
    => await SendBroadcast([adjacencyLists]);

    private static InvocationResultDto<T> Ok<T>(T response) => new SuccessResultDto<T>(response);

    private static Task<InvocationResultDto<T>> OkAsync<T>(T response) => Task.FromResult(Ok(response));

    private static InvocationResultDto<T> Error<T>(T? response, string error) => ErrorResultDto<T>.Create(response, error);

    private static Task<InvocationResultDto<T>> ErrorAsync<T>(T? response, string error) => Task.FromResult(Error(response, error));

    private static InvocationResultDto<T> Error<T>(string error) => Error<T>(default, error);

    private static Task<InvocationResultDto<T>> ErrorAsync<T>(string error)
    => Task.FromResult(Error<T>(error));
}
