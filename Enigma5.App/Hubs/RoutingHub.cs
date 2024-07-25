using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Sessions;
using Enigma5.App.Resources.Commands;
using MediatR;
using Enigma5.App.Resources.Queries;
using Enigma5.App.Models;
using Enigma5.Crypto;
using Enigma5.App.Security.Contracts;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Data;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs;

public partial class RoutingHub(
    SessionManager sessionManager,
    ICertificateManager certificateManager,
    NetworkGraph networkGraph,
    IMediator commandRouter) :
    Hub,
    IHub,
    IOnionParsingHub,
    IOnionRoutingHub
{
    private readonly SessionManager _sessionManager = sessionManager;

    private readonly ICertificateManager _certificateManager = certificateManager;

    private readonly NetworkGraph _networkGraph = networkGraph;

    private readonly IMediator _commandRouter = commandRouter;

    public string? DestinationConnectionId { get; set; }

    public int Size { get; set; }

    public string? Next { get; set; }

    public byte[]? Content { get; set; }

    public Task<InvocationResult<string>> GenerateToken()
    {
        var nonce = _sessionManager.AddPending(Context.ConnectionId);

        return nonce is not null
        ? OkAsync(nonce)
        : ErrorAsync<string>(InvocationErrors.NONCE_GENERATION_ERROR);
    }

    public async Task Synchronize()
    {
        if (_sessionManager.TryGetAddress(Context.ConnectionId, out string? address))
        {
            var onions = await _commandRouter.Send(new GetPendingMessagesByDestinationQuery(address!));

            if (onions.Any())
            {
                try
                {
                    var pendingMessages = onions.Select(item => new Models.PendingMessage
                    {
                        Destination = item.Destination,
                        Content = item.Content,
                        DateReceived = item.DateReceived
                    });
                    await RespondAsync(nameof(Synchronize), pendingMessages);
                }
                catch
                {
                    return;
                }

                await _commandRouter.Send(new RemoveMessagesCommand(address!));
            }
        }
    }

    [ValidateModel]
    public async Task<InvocationResult<bool>> Authenticate(AuthenticationRequest request)
    {
        var authenticated = _sessionManager.Authenticate(Context.ConnectionId, request.PublicKey!, request.Signature!);

        if (!authenticated)
        {
            return Error<bool>(InvocationErrors.INVALID_NONCE_SIGNATURE);
        }

        if (request.SyncMessagesOnSuccess)
        {
            await Synchronize();
        }

        if (request.UpdateNetworkGraph)
        {
            await AddNewAdjacency(request.PublicKey!);
        }

        return Ok(true);
    }

    [ValidateModel]
    public Task<InvocationResult<Signature>> SignToken(SignatureRequest request)
    {
        using var signature = Envelope.Factory.CreateSignature(_certificateManager.PrivateKey, string.Empty);
        var data = signature.Sign(Convert.FromBase64String(request.Nonce!));

        if (data == null)
        {
            return ErrorAsync<Signature>(InvocationErrors.NONCE_SIGNATURE_FAILED);
        }

        var encodedData = Convert.ToBase64String(data);

        if (encodedData is null)
        {
            return ErrorAsync<Signature>(InvocationErrors.NONCE_SIGNATURE_FAILED);
        }

        return OkAsync<Signature>(new(encodedData, _certificateManager.PublicKey));
    }

    [ValidateModel]
    public async Task<InvocationResult<bool>> Broadcast(VertexBroadcastRequest broadcastAdjacencyList)
    {
        var (localVertex, broadcasts) = await _commandRouter.Send(new HandleBroadcastCommand(broadcastAdjacencyList));

        if (localVertex != null && broadcasts != null)
        {
            return await SendBroadcast(broadcasts)
            ? Ok(true)
            : Error<bool>(InvocationErrors.BROADCAST_FORWARDING_ERROR);
        }

        return Error<bool>(InvocationErrors.BROADCAST_HANDLING_ERROR);
    }

    [AuthorizedServiceOnly]
    public async Task<InvocationResult<bool>> TriggerBroadcast()
    {
        var localVertex = _networkGraph.LocalVertex;
        var broadcast = Vertex.ToBroadcast(localVertex);

        return await SendBroadcast(broadcast)
        ? Ok(true)
        : Error<bool>(InvocationErrors.BROADCAST_TRIGGERING_FAILED);
    }

    [ValidateModel]
    [OnionParsing]
    [OnionRouting]
    public async Task<InvocationResult<bool>> RouteMessage(RoutingRequest request)
    {
        if (DestinationConnectionId != null && Content != null)
        {
            return await RouteMessage(DestinationConnectionId, Content) ? Ok(true) : Error(false, "");
        }
        else if (Content != null)
        {
            var encodedContent = Convert.ToBase64String(Content);

            var createPendingMessageCommand = new CreatePendingMessageCommand(Next!, encodedContent);

            return await _commandRouter.Send(createPendingMessageCommand) is not null ? Ok(true) : Error(false, "");
        }
        return Error(false, "");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (!_sessionManager.Remove(Context.ConnectionId, out string? removedAddress))
        {
            return;
        }

        var (_, broadcast) = await RemoveAdjacency(removedAddress!);

        if (broadcast != null)
        {
            await SendBroadcast(broadcast);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
