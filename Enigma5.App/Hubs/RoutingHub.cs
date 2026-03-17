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

namespace Enigma5.App.Hubs;

public partial class RoutingHub(
    ISessionManager sessionManager,
    ICertificateManager certificateManager,
    IMediator commandRouter,
    IConfiguration configuration,
    ILogger<RoutingHub> logger) :
    Hub,
    IEnigmaHub,
    IOnionParsingHub,
    IOnionRoutingHub,
    IIdentityHub,
    IAuthorizedServiceHub
{
    private readonly ISessionManager _sessionManager = sessionManager;

    private readonly ICertificateManager _certificateManager = certificateManager;

    private readonly IMediator _commandRouter = commandRouter;

    private readonly ILogger<RoutingHub> _logger = logger;

    private readonly IConfiguration _configuration = configuration;

    public string? DestinationConnectionId { get; set; }

    public string? Next { get; set; }

    public byte[]? Content { get; set; }

    public string? ClientAddress { get; set; }

    public Task<InvocationResultDto<string>> GenerateToken()
    {
        var nonce = _sessionManager.AddPending(Context.ConnectionId);

        if (nonce is null)
        {
            _logger.LogError(
                $"Null nonce generated while invoking {{{Common.Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Common.Constants.Serilog.ConnectionIdKey}}}.",
                nameof(GenerateToken),
                Context.ConnectionId
                );
        }

        return nonce is not null ? OkAsync(nonce) : ErrorAsync<string>(InvocationErrors.NONCE_GENERATION_ERROR);
    }

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

    [Authenticated]
    public async Task<InvocationResultDto<List<PendingMessageDto>>> Pull()
    {
        if (ClientAddress is null)
        {
            _logger.LogError($"ClientAddress null while invoking {{{Common.Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Common.Constants.Serilog.ConnectionIdKey}}}.",
            nameof(Pull),
            Context.ConnectionId);
            return Error<List<PendingMessageDto>>(InvocationErrors.INTERNAL_ERROR);
        }

        var result = await _commandRouter.Send(new GetPendingMessagesByDestinationQuery(ClientAddress));

        if (result.IsSuccessNotNullResultValue())
        {
            await _commandRouter.Send(new MarkMessagesAsDeliveredCommand(ClientAddress));
            return Ok(result.Value!);
        }

        _logger.LogError($"Could not retrieve pending messages while invoking {{{Common.Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Common.Constants.Serilog.ConnectionIdKey}}}; Command result: {{@{Common.Constants.Serilog.CommandResultKey}}}.",
        nameof(Pull),
        Context.ConnectionId,
        result);
        return Error<List<PendingMessageDto>>(InvocationErrors.INTERNAL_ERROR);
    }

    [Authenticated]
    public async Task<InvocationResultDto<bool>> Cleanup()
    {
        if (ClientAddress is null)
        {
            _logger.LogError($"ClientAddress null while invoking {{{Common.Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Common.Constants.Serilog.ConnectionIdKey}}}.",
            nameof(Cleanup),
            Context.ConnectionId);
            return Error<bool>(InvocationErrors.INTERNAL_ERROR);
        }

        var result = await _commandRouter.Send(new RemoveMessagesCommand(ClientAddress));

        if (result.IsSuccessNotNullResultValue())
        {
            return Ok(true);
        }

        _logger.LogError($"Could cleanup pending messages while invoking {{{Common.Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Common.Constants.Serilog.ConnectionIdKey}}}; Command result: {{@{Common.Constants.Serilog.CommandResultKey}}}.",
        nameof(Pull),
        Context.ConnectionId,
        result);
        return Error<bool>(InvocationErrors.INTERNAL_ERROR);
    }

    [ValidateModel]
    public async Task<InvocationResultDto<bool>> Authenticate(AuthenticationRequestDto request)
    {
        var authenticated = await Authenticate(request.PublicKey!, request.Signature!);

        if (!authenticated)
        {
            _logger.LogDebug($"Could not authenticate connectionId {{{Common.Constants.Serilog.ConnectionIdKey}}}.", Context.ConnectionId);
            return Error<bool>(InvocationErrors.INVALID_NONCE_SIGNATURE);
        }

        _logger.LogDebug($"ConnectionId {{{Common.Constants.Serilog.ConnectionIdKey}}} authenticated.", Context.ConnectionId);
        return Ok(true);
    }

    [Authenticated]
    [ValidateModel]
    public async Task<InvocationResultDto<bool>> Broadcast(VertexBroadcastRequestDto broadcastAdjacencyList)
    {
        var result = await _commandRouter.Send(new HandleBroadcastCommand(broadcastAdjacencyList));

        if (result.IsSuccessNotNullResultValue())
        {
            _logger.LogError($"Invocation of {{{Common.Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Common.Constants.Serilog.ConnectionIdKey}}} completed with no success.", nameof(Broadcast), Context.ConnectionId);
            return await SendBroadcast(result.Value!)
            ? Ok(true)
            : Error<bool>(InvocationErrors.BROADCAST_FORWARDING_ERROR);
        }

        return Error<bool>(InvocationErrors.BROADCAST_HANDLING_ERROR);
    }

    [ValidateModel]
    [Authenticated]
    [AuthorizedServiceOnly]
    public async Task<InvocationResultDto<bool>> TriggerBroadcast(TriggerBroadcastRequestDto request)
    {
        var localVertex = await AddNewAdjacencies(request.NewAddresses ?? []);
        await SyncPendingMessages();

        if (localVertex is null)
        {
            _logger.LogError($"Invocation of {{{Common.Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Common.Constants.Serilog.ConnectionIdKey}}} resulted in no changes to be broadcasted.", nameof(TriggerBroadcast), Context.ConnectionId);
            return Error(true, InvocationErrors.BROADCAST_TRIGGERING_WARNING);
        }

        return await SendBroadcast(localVertex!)
            ? Ok(true)
            : Error<bool>(InvocationErrors.BROADCAST_TRIGGERING_FAILED);
    }

    [ValidateModel]
    [OnionParsing]
    [OnionRouting]
    [Authenticated]
    public async Task<InvocationResultDto<bool>> RouteMessage(RoutingRequestDto request)
    {
        bool success = false;
        if (Content is not null)
        {
            var result = await CreatePendingMessage();
            success = result.IsSuccessNotNullResultValue();
            if (DestinationConnectionId != null)
            {
                success |= await RouteMessage(DestinationConnectionId, Content, result?.Value?.Uuid);
            }
        }
        return success ? Ok(true) : Error<bool>(InvocationErrors.ONION_ROUTING_FAILED);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug($"ConnectionId {{{Common.Constants.Serilog.ConnectionIdKey}}} disconnected.", Context.ConnectionId);
        if (!_sessionManager.Remove(Context.ConnectionId, out string? removedAddress))
        {
            _logger.LogError($"ConnectionId {{{Common.Constants.Serilog.ConnectionIdKey}}} disconnected, but the connection could not be found into Session Manager.", Context.ConnectionId);
            return;
        }

        var broadcast = await RemoveAdjacencies([removedAddress!]);

        if (broadcast != null)
        {
            await SendBroadcast(broadcast);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public override async Task OnConnectedAsync()
    {
        var impersonateServiceHeader = Context.GetHttpContext()?.Request.Headers[Common.Constants.XImpersonateServiceHeader];
        if (!impersonateServiceHeader.HasValue)
        {
            return;
        }
        var impersonateServiceHeaderString = impersonateServiceHeader.ToString();
        if (string.IsNullOrWhiteSpace(impersonateServiceHeaderString))
        {
            return;
        }
        Context.Items[Common.Constants.XImpersonateServiceHeader] = impersonateServiceHeaderString;
        await base.OnConnectedAsync();
    }
}
