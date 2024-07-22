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

    public string? GenerateToken()
    => _sessionManager.AddPending(Context.ConnectionId);

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

    public async Task<bool> Authenticate(AuthenticationRequest request)
    {
        if (request.PublicKey == null || request.Signature == null)
        {
            return false;
        }

        var authenticated = _sessionManager.Authenticate(Context.ConnectionId, request.PublicKey, request.Signature);

        if (!authenticated)
        {
            return false;
        }

        if (request.SyncMessagesOnSuccess)
        {
            await Synchronize();
        }

        if (request.UpdateNetworkGraph)
        {
            await AddNewAdjacency(request.PublicKey);
        }

        return true;
    }

    public Signature? SignToken(string token)
    {
        if (token == null)
        {
            return null;
        }

        using var signature = Envelope.Factory.CreateSignature(_certificateManager.PrivateKey, string.Empty);
        var data = signature.Sign(Convert.FromBase64String(token));

        if (data == null)
        {
            return null;
        }

        return new Signature(Convert.ToBase64String(data), _certificateManager.PublicKey);
    }

    public async Task<bool> Broadcast(VertexBroadcast broadcastAdjacencyList)
    {
        var (localVertex, broadcasts) = await _commandRouter.Send(new HandleBroadcastCommand(broadcastAdjacencyList));

        if (localVertex != null && broadcasts != null)
        {
            return await SendBroadcast(broadcasts);
        }

        return false;
    }

    [AuthorizedServiceOnly]
    public Task<bool> TriggerBroadcast()
    {
        var localVertex = _networkGraph.LocalVertex;
        var broadcast = Vertex.ToBroadcast(localVertex);

        return SendBroadcast(broadcast);
    }

    [OnionParsing]
    [OnionRouting]
    public async Task<bool> RouteMessage(string data)
    {
        if (DestinationConnectionId != null && Content != null)
        {
            return await RouteMessage(DestinationConnectionId, Content);
        }
        else if (Content != null)
        {
            var encodedContent = Convert.ToBase64String(Content);

            var createPendingMessageCommand = new CreatePendingMessageCommand(Next!, encodedContent);

            return await _commandRouter.Send(createPendingMessageCommand) is not null;
        }
        return false;
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
