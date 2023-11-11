using AutoMapper;
using Enigma5.App.Data;
using Enigma5.App.Network;
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class SynchronizeConnectionsHandler
: IRequestHandler<SynchronizeConnectionsCommand>
{
    private readonly NetworkGraph _networkGraph;

    private readonly NetworkBridge _networkBridge;

    private readonly IMapper _mapper;

    public SynchronizeConnectionsHandler(NetworkGraph networkGraph, NetworkBridge networkBridge, IMapper mapper)
    {
        _networkBridge = networkBridge;
        _networkGraph = networkGraph;
        _mapper = mapper;
    }

    public async Task Handle(SynchronizeConnectionsCommand request, CancellationToken cancellationToken)
    {
        if (request.BroadcastAdjacencyList == null)
        {
            return;
        }

        var vertex = _mapper.Map<Vertex>(request.BroadcastAdjacencyList);
        var (_, delta) = await _networkGraph.AddAsync(vertex, cancellationToken);

        if (delta.Vertex != null && delta.Vertex.Neighborhood.Hostname != null && delta.Added)
        {
            if (await _networkBridge.ConnectPeerAsync(delta.Vertex.Neighborhood.Hostname, delta.Vertex.Neighborhood.Address))
            {
                await _networkBridge.BroadcastAdjacencyListAsync();
                await _networkBridge.BroadcastAdjacencyListAsync(request.BroadcastAdjacencyList);
            }
            else
            {
                _networkGraph.Revert(delta);
            }
        }
        else if (delta.Vertex != null && !delta.Added)
        {
            await _networkBridge.CloseConnectionAsync(delta.Vertex.Neighborhood.Address, cancellationToken);
        }
    }
}
