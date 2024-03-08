using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Sessions;
using Enigma5.App.Resources.Commands;
using MediatR;
using Enigma5.App.Resources.Queries;
using Enigma5.App.Models;
using Enigma5.Crypto;
using Enigma5.App.Security;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Data;
using AutoMapper;

namespace Enigma5.App.Hubs;

public class RoutingHub(
    SessionManager sessionManager,
    CertificateManager certificateManager,
    NetworkGraph networkGraph,
    IMediator commandRouter,
    IMapper mapper) :
    RoutingHubBase<RoutingHub>,
    IHub,
    IOnionParsingHub,
    IOnionRoutingHub
{
    private readonly SessionManager _sessionManager = sessionManager;

    private readonly CertificateManager _certificateManager = certificateManager;

    private readonly NetworkGraph _networkGraph = networkGraph;

    private readonly IMediator _commandRouter = commandRouter;

    private readonly IMapper _mapper = mapper;

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
            var onions = await _commandRouter.Send(new GetPendingMessagesByDestinationQuery
            {
                Destination = address!
            });

            if (onions.Any())
            {
                try
                {
                    await RespondAsync(nameof(Synchronize), _mapper.Map<List<Models.PendingMessage>>(onions));
                }
                catch
                {
                    return;
                }

                await _commandRouter.Send(new RemoveMessagesCommand(address!));
            }
        }
    }

    private async Task<(Vertex localVertex, BroadcastAdjacencyList? broadcast)> AddNewAdjacency(string publicKey)
    {
        var command = new UpdateLocalAdjacencyCommand(CertificateHelper.GetHexAddressFromPublicKey(publicKey), true);

        return await _commandRouter.Send(command);
    }

    private async Task<(Vertex localVertex, BroadcastAdjacencyList? broadcast)> RemoveAdjacency(string address)
    {
        var command = new UpdateLocalAdjacencyCommand(address, false);

        return await _commandRouter.Send(command);
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

    private async Task SendBroadcast(IEnumerable<BroadcastAdjacencyList> broadcasts, IEnumerable<string> addresses)
    {
        foreach (var address in addresses)
        {
            if (_sessionManager.TryGetConnectionId(address, out string? connectionId))
            {
                foreach (var broadcast in broadcasts)
                {
                    await SendAsync(connectionId!, nameof(Broadcast), broadcast);
                }
            }
        }
    }

    private async Task SendBroadcast(BroadcastAdjacencyList broadcast, IEnumerable<string> addresses)
    => await SendBroadcast([broadcast], addresses);

    public async Task Broadcast(BroadcastAdjacencyList broadcastAdjacencyList)
    {
        var (localVertex, broadcasts) = await _commandRouter.Send(new HandleBroadcastCommand(broadcastAdjacencyList));

        if (localVertex != null && broadcasts != null)
        {
            await SendBroadcast(broadcasts, localVertex.Neighborhood.Neighbors);
        }
    }

    public async Task TriggerBroadcast()
    {
        if (_sessionManager.TryGetAddress(Context.ConnectionId, out _))
        {
            var localVertex = _networkGraph.LocalVertex;
            var broadcast = _mapper.Map<BroadcastAdjacencyList>(localVertex);

            await SendBroadcast(broadcast, localVertex.Neighborhood.Neighbors);
        }
    }

    [OnionParsing]
    [OnionRouting]
    public async Task RouteMessage(string data)
    {
        if (DestinationConnectionId != null && Content != null)
        {
            await RouteMessage(DestinationConnectionId, Content);
        }
        else if (Content != null)
        {
            var encodedContent = Convert.ToBase64String(Content);

            var createPendingMessageCommand = new CreatePendingMessageCommand(Next!, encodedContent);

            await _commandRouter.Send(createPendingMessageCommand);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (!_sessionManager.Remove(Context.ConnectionId, out string? removedAddress))
        {
            return;
        }

        var (localVertex, broadcast) = await RemoveAdjacency(removedAddress!);

        if (broadcast != null)
        {
            await SendBroadcast(broadcast, localVertex.Neighborhood.Neighbors);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
