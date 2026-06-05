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

using Enigma5.App.Attributes;
using Enigma5.App.Resources.Commands;
using MediatR;
using Enigma5.App.Resources.Queries;
using Enigma5.App.Models;
using Enigma5.Security.Contracts;
using Microsoft.AspNetCore.SignalR;
using Enigma5.App.Models.HubInvocation;
using Enigma5.App.Resources.Handlers;
using Enigma5.App.Hubs.Sessions.Contracts;
using Enigma5.App.Models.Contracts.Hubs;
using Enigma5.App.Extensions;
using Enigma5.App.Common;

namespace Enigma5.App.Hubs;

public partial class RoutingHub(
    ISessionManager sessionManager,
    ICertificateManager certificateManager,
    IMediator commandRouter,
    ILogger<RoutingHub> logger) :
    Hub,
    IEnigmaHub,
    IOnionParsingHub,
    IOnionRoutingHub,
    IIdentityHub
{
    private readonly ISessionManager _sessionManager = sessionManager;

    private readonly ICertificateManager _certificateManager = certificateManager;

    private readonly IMediator _commandRouter = commandRouter;

    private readonly ILogger<RoutingHub> _logger = logger;

    public string? DestinationConnectionId { get; set; }

    public string? Next { get; set; }

    public byte[]? Content { get; set; }

    public string? Uuid { get; set; }

    public string? ClientAddress { get; set; }

    [BlacklistAuthorization]
    public async Task<InvocationResultDto<string>> GenerateToken()
    {
        var nonce = await _sessionManager.AddPendingAsync(Context.ConnectionId);

        if (nonce is null)
        {
            _logger.LogError(
                $"Null nonce generated while invoking {{{Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Constants.Serilog.ConnectionIdKey}}}.",
                nameof(GenerateToken),
                Context.ConnectionId
                );
        }

        return nonce is not null ? Ok(nonce) : Error<string>(InvocationErrors.NONCE_GENERATION_ERROR);
    }

    [BlacklistAuthorization]
    public async Task<InvocationResultDto<VertexDto>> GetLocalVertex()
    {
        var localAddress = await _certificateManager.GetAddressAsync();
        if (string.IsNullOrWhiteSpace(localAddress))
        {
            return Error<VertexDto>(InvocationErrors.INTERNAL_ERROR);
        }
        var result = await _commandRouter.Send(new GetVertexQuery(localAddress));
        if (!result.IsSuccessNotNullResultValue())
        {
            return Error<VertexDto>(InvocationErrors.INTERNAL_ERROR);
        }
        return Ok(result.Value!);
    }

    [Obsolete("Use PullPaged instead; Still here for compatibility with previous versions and will be removed in the future;")]
    [Authenticated]
    [BlacklistAuthorization]
    public async Task<InvocationResultDto<List<PendingMessageDto>>> Pull()
    {
        if (ClientAddress is null)
        {
            _logger.LogError($"ClientAddress null while invoking {{{Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Constants.Serilog.ConnectionIdKey}}}.",
            nameof(Pull),
            Context.ConnectionId);
            return Error<List<PendingMessageDto>>(InvocationErrors.INTERNAL_ERROR);
        }

        return Ok(await GetPendingMessagesAsync(ClientAddress, null, 1024));
    }

    [Authenticated]
    [BlacklistAuthorization]
    public async Task<InvocationResultDto<List<PendingMessageDto>>> Pull2(PullRequestDto request)
    {
        if (ClientAddress is null)
        {
            _logger.LogError($"ClientAddress null while invoking {{{Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Constants.Serilog.ConnectionIdKey}}}.",
            nameof(Pull),
            Context.ConnectionId);
            return Error<List<PendingMessageDto>>(InvocationErrors.INTERNAL_ERROR);
        }

        return Ok(await GetPendingMessagesAsync(ClientAddress, request.InfId, Constants.MessagesPageSize));
    }

    [Authenticated]
    [BlacklistAuthorization]
    public async Task<InvocationResultDto<bool>> Cleanup()
    {
        if (ClientAddress is null)
        {
            _logger.LogError($"ClientAddress null while invoking {{{Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Constants.Serilog.ConnectionIdKey}}}.",
            nameof(Cleanup),
            Context.ConnectionId);
            return Error<bool>(InvocationErrors.INTERNAL_ERROR);
        }

        var result = await _commandRouter.Send(new MarkMessagesAsDeliveredCommand(ClientAddress, null));

        if (result.IsSuccessNotNullResultValue())
        {
            return Ok(true);
        }

        _logger.LogError($"Could cleanup pending messages while invoking {{{Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Constants.Serilog.ConnectionIdKey}}}; Command result: {{@{Constants.Serilog.CommandResultKey}}}.",
        nameof(Pull),
        Context.ConnectionId,
        result);
        return Error<bool>(InvocationErrors.INTERNAL_ERROR);
    }

    [ValidateModel]
    [BlacklistAuthorization]
    public async Task<InvocationResultDto<bool>> Authenticate(AuthenticationRequestDto request)
    {
        var authenticated = await Authenticate(request.PublicKey!, request.Signature!);

        if (!authenticated)
        {
            _logger.LogDebug($"Could not authenticate connectionId {{{Constants.Serilog.ConnectionIdKey}}}.", Context.ConnectionId);
            return Error<bool>(InvocationErrors.INVALID_NONCE_SIGNATURE);
        }

        _logger.LogDebug($"ConnectionId {{{Constants.Serilog.ConnectionIdKey}}} authenticated.", Context.ConnectionId);
        return Ok(true);
    }

    [Authenticated]
    [ValidateModel]
    [BlacklistAuthorization]
    public async Task<InvocationResultDto<bool>> Broadcast(VertexBroadcastRequestDto broadcastAdjacencyList)
    {
        var result = await _commandRouter.Send(new HandleBroadcastCommand(broadcastAdjacencyList));

        if (result.IsSuccessNotNullResultValue())
        {
            return await SendBroadcast(result.Value!)
            ? Ok(true)
            : Error<bool>(InvocationErrors.BROADCAST_FORWARDING_ERROR);
        }

        return Error<bool>(InvocationErrors.BROADCAST_HANDLING_ERROR);
    }

    [ValidateModel]
    [Authenticated]
    [BlacklistAuthorization]
    public async Task<InvocationResultDto<bool>> TriggerBroadcast(TriggerBroadcastRequestDto request)
    {
        var vertexBroadcastRequest = await AddNewAdjacencies(request.NewAddresses ?? []);

        if (vertexBroadcastRequest is null)
        {
            _logger.LogError($"Invocation of {{{Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Constants.Serilog.ConnectionIdKey}}} returned null vertex broadcast.",
            nameof(TriggerBroadcast),
            Context.ConnectionId);
            return Error(true, InvocationErrors.BROADCAST_TRIGGERING_WARNING);
        }

        return await SendBroadcast(vertexBroadcastRequest!)
            ? Ok(true)
            : Error<bool>(InvocationErrors.BROADCAST_TRIGGERING_FAILED);
    }

    [ValidateModel]
    [OnionParsing]
    [OnionRouting]
    [Authenticated]
    [BlacklistAuthorization]
    public async Task<InvocationResultDto<bool>> RouteMessage(RoutingRequestDto request)
    {
        if (Content is not null)
        {
            var result = await CreatePendingMessage();
            if (DestinationConnectionId != null && result.IsSuccessNotNullResultValue())
            {
                await RouteMessage(DestinationConnectionId, Content, result?.Value?.Uuid);
            }
            var success = (Uuid == null && result?.Value?.Uuid != null) || (Uuid != null && Uuid == result?.Value?.Uuid);
            return success ? Ok(true) : Error<bool>(InvocationErrors.ONION_ROUTING_FAILED);
        }
        return Error<bool>(InvocationErrors.ONION_ROUTING_FAILED);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug($"ConnectionId {{{Constants.Serilog.ConnectionIdKey}}} disconnected.", Context.ConnectionId);
        var removedAddress = await _sessionManager.RemoveAsync(Context.ConnectionId);
        if (removedAddress == null)
        {
            _logger.LogError($"ConnectionId {{{Constants.Serilog.ConnectionIdKey}}} disconnected, but the connection could not be found into Session Manager.", Context.ConnectionId);
            return;
        }

        await RemoveAdjacencies([removedAddress!]);

        await base.OnDisconnectedAsync(exception);
    }

    public override async Task OnConnectedAsync()
    {
        Context.MapConnectionDetails();
        await base.OnConnectedAsync();
    }
}
