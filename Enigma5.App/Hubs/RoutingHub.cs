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

using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Sessions;
using Enigma5.App.Resources.Commands;
using MediatR;
using Enigma5.App.Resources.Queries;
using Enigma5.App.Models;
using Enigma5.Security.Contracts;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Data;
using Microsoft.AspNetCore.SignalR;
using Enigma5.App.Data.Extensions;
using Enigma5.App.Models.HubInvocation;
using Microsoft.Extensions.Logging;
using Enigma5.App.Resources.Handlers;
using Enigma5.Crypto;
using Enigma5.App.Hubs.Sessions.Contracts;

namespace Enigma5.App.Hubs;

public partial class RoutingHub(
    ISessionManager sessionManager,
    ICertificateManager certificateManager,
    NetworkGraph networkGraph,
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

    private readonly NetworkGraph _networkGraph = networkGraph;

    private readonly IMediator _commandRouter = commandRouter;

    private readonly ILogger<RoutingHub> _logger = logger;

    public string? DestinationConnectionId { get; set; }

    public string? Next { get; set; }

    public byte[]? Content { get; set; }

    public string? ClientAddress { get; set; }

    public Task<InvocationResult<string>> GenerateToken()
    {
        var nonce = _sessionManager.AddPending(Context.ConnectionId);

        if (nonce is null)
        {
            _logger.LogError(
                $"Null nonce generated while invoking {{{nameof(HubInvocationContext.HubMethodName)}}} for {{{nameof(Context.ConnectionId)}}}.",
                nameof(GenerateToken),
                Context.ConnectionId
                );
        }

        return nonce is not null ? OkAsync(nonce) : ErrorAsync<string>(InvocationErrors.NONCE_GENERATION_ERROR);
    }

    [Authenticated]
    [AuthorizedServiceOnly]
    public async Task Synchronize()
    {
        if (ClientAddress is null)
        {
            _logger.LogError($"ClientAddress null while invoking {{{nameof(HubInvocationContext.HubMethodName)}}} for {{{nameof(Context.ConnectionId)}}}.",
            nameof(Synchronize),
            Context.ConnectionId);
            return;
        }

        var result = await _commandRouter.Send(new GetPendingMessagesByDestinationQuery(ClientAddress));

        if (result.IsSuccessNotNullResultValue() && await RespondAsync(nameof(Synchronize), result.Value))
        {
            await _commandRouter.Send(new RemoveMessagesCommand(ClientAddress));
        }
    }

    [Authenticated]
    public async Task<InvocationResult<List<Models.PendingMessage>>> Pull()
    {
        if (ClientAddress is null)
        {
            _logger.LogError($"ClientAddress null while invoking {{{nameof(HubInvocationContext.HubMethodName)}}} for {{{nameof(Context.ConnectionId)}}}.",
            nameof(Pull),
            Context.ConnectionId);
            return Error<List<Models.PendingMessage>>(InvocationErrors.INTERNAL_ERROR);
        }

        var result = await _commandRouter.Send(new GetPendingMessagesByDestinationQuery(ClientAddress));

        if (result.IsSuccessNotNullResultValue())
        {
            await _commandRouter.Send(new RemoveMessagesCommand(ClientAddress));
            return Ok(result.Value!);
        }

        _logger.LogError($"Could not retrieve pending messages while invoking {{{nameof(HubInvocationContext.HubMethodName)}}} for {{{nameof(Context.ConnectionId)}}}; Command result: {{result}}.",
        nameof(Pull),
        Context.ConnectionId,
        result);
        return Error<List<Models.PendingMessage>>(InvocationErrors.INTERNAL_ERROR);
    }

    [ValidateModel]
    public Task<InvocationResult<bool>> Authenticate(AuthenticationRequest request)
    {
        var authenticated = _sessionManager.Authenticate(Context.ConnectionId, request.PublicKey!, request.Signature!);

        if (!authenticated)
        {
            _logger.LogDebug($"Could not authenticate connectionId {{{nameof(Context.ConnectionId)}}}.", Context.ConnectionId);
            return ErrorAsync<bool>(InvocationErrors.INVALID_NONCE_SIGNATURE);
        }

        _logger.LogDebug($"ConnectionId {{{nameof(Context.ConnectionId)}}} authenticated.", Context.ConnectionId);
        return OkAsync(true);
    }

    [ValidateModel]
    public Task<InvocationResult<Signature>> SignToken(SignatureRequest request)
    {
        var decodedNonce = Convert.FromBase64String(request.Nonce!);
        if (decodedNonce is null)
        {
            _logger.LogDebug($"Could not base64 decode nonce for connectionId {{{nameof(Context.ConnectionId)}}}.", Context.ConnectionId);
            return ErrorAsync<Signature>(InvocationErrors.NONCE_SIGNATURE_FAILED);
        }

        var signer = SealProvider.Factory.CreateSigner(_certificateManager.PrivateKey);
        var data = signer.Sign(decodedNonce);

        if (data == null)
        {
            _logger.LogError($"Could not sign nonce for connectionId {{{nameof(Context.ConnectionId)}}}.", Context.ConnectionId);
            return ErrorAsync<Signature>(InvocationErrors.NONCE_SIGNATURE_FAILED);
        }

        var encodedData = Convert.ToBase64String(data);

        if (encodedData is null)
        {
            _logger.LogError($"Could not base64 encode signed nonce for connectionId {{{nameof(Context.ConnectionId)}}}.", Context.ConnectionId);
            return ErrorAsync<Signature>(InvocationErrors.NONCE_SIGNATURE_FAILED);
        }

        return OkAsync<Signature>(new(encodedData, _certificateManager.PublicKey));
    }

    [ValidateModel]
    public async Task<InvocationResult<bool>> Broadcast(VertexBroadcastRequest broadcastAdjacencyList)
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
    [AuthorizedServiceOnly]
    public async Task<InvocationResult<bool>> TriggerBroadcast(TriggerBroadcastRequest request)
    {
        var vertexBroadcastRequest = request.NewAddresses is null || request.NewAddresses.Count == 0
        ? _networkGraph.LocalVertex.ToVertexBroadcast()
        : (await AddNewAdjacencies(request.NewAddresses));

        if (vertexBroadcastRequest is null)
        {
            _logger.LogWarning($"{nameof(TriggerBroadcast)} resulted in no changes to be broadcasted.");
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
    public async Task<InvocationResult<bool>> RouteMessage(RoutingRequest request)
    {
        if (DestinationConnectionId != null && Content != null)
        {
            return await RouteMessage(DestinationConnectionId, Content) ? Ok(true) : Error(false, InvocationErrors.ONION_ROUTING_FAILED);
        }
        else if (Content != null)
        {
            _logger.LogDebug($"Onion could not be forwarded for connectionId {{{nameof(Context.ConnectionId)}}}. Saving locally...", Context.ConnectionId);
            var encodedContent = Convert.ToBase64String(Content);
            if (encodedContent is null)
            {
                _logger.LogError($"Could not base64 onion content for connectionId {{{nameof(Context.ConnectionId)}}}", Context.ConnectionId);
                return Error<bool>(InvocationErrors.ONION_ROUTING_FAILED);
            }
            var result = await _commandRouter.Send(new CreatePendingMessageCommand(Next!, encodedContent));

            return result.IsSuccessNotNullResultValue() ? Ok(true) : Error(false, InvocationErrors.ONION_ROUTING_FAILED);
        }
        return Error<bool>(InvocationErrors.ONION_ROUTING_FAILED);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug($"ConnectionId {{{nameof(Context.ConnectionId)}}} disconnected.", Context.ConnectionId);
        if (!_sessionManager.Remove(Context.ConnectionId, out string? removedAddress))
        {
            _logger.LogWarning($"ConnectionId {{{nameof(Context.ConnectionId)}}} disconnected, but the connection could not be found into Session Manager", Context.ConnectionId);
            return;
        }

        var broadcast = await RemoveAdjacencies([removedAddress!]);

        if (broadcast != null)
        {
            await SendBroadcast(broadcast);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
