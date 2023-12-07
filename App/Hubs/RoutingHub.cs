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

public class RoutingHub :
    RoutingHubBase<RoutingHub>,
    IHub,
    IOnionParsingHub,
    IOnionRoutingHub
{
    private readonly SessionManager _sessionManager;

    private readonly CertificateManager _certificateManager;

    private readonly NetworkGraph _networkGraph;

    private readonly IMediator _commandRouter;

    private readonly IMapper _mapper;

    public RoutingHub(
        SessionManager sessionManager,
        CertificateManager certificateManager,
        NetworkGraph networkGraph,
        IMediator commandRouter,
        IMapper mapper)
    {
        _sessionManager = sessionManager;
        _certificateManager = certificateManager;
        _networkGraph = networkGraph;
        _commandRouter = commandRouter;
        _mapper = mapper;
    }

    public string? Address { get; set; }

    public string? DestinationConnectionId { get; set; }

    public int Size { get; set; }

    public string? Next { get; set; }

    public byte[]? Content { get; set; }

    public string? GenerateToken()
    => _sessionManager.AddPending(Context.ConnectionId);

    private async Task Synchronize()
    {
        if (_sessionManager.TryGetAddress(Context.ConnectionId, out string? address))
        {
            var query = new GetPendingMessagesByDestinationQuery
            {
                Destination = address!
            };

            var onions = await _commandRouter.Send(query);

            if (onions.Any())
            {
                await RespondAsync(nameof(Synchronize), onions.Select(item => new Models.PendingMessage
                {
                    Content = item.Content,
                    Destination = item.Destination,
                    DateReceived = item.DateReceived
                }));

                var command = new MarkMessagesAsDeliveredCommand(address!);

                await _commandRouter.Send(command);
            }
        }
    }

    private async Task<(Vertex? localVertex, BroadcastAdjacencyList? broadcasts)> AddNewAdjacency(string publicKey)
    {
        var command = new UpdateLocalAdjacencyCommand(CertificateHelper.GetHexAddressFromPublicKey(publicKey), true);

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
    => await SendBroadcast(new List<BroadcastAdjacencyList> { broadcast }, addresses);

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
        var removedAddress = _sessionManager.Remove(Context.ConnectionId);

        if(removedAddress == null)
        {
            return;
        }

        var command = new UpdateLocalAdjacencyCommand(removedAddress, false);

        var (localVertex, broadcast) = await _commandRouter.Send(command);

        if (broadcast != null)
        {
            await SendBroadcast(broadcast, localVertex.Neighborhood.Neighbors);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
