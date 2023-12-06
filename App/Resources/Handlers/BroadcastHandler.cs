using AutoMapper;
using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class BroadcastHandler
: IRequestHandler<HandleBroadcastCommand, (Vertex? localVertex, IEnumerable<BroadcastAdjacencyList>? broadcasts)>
{
    private readonly NetworkGraph _networkGraph;

    private readonly IMapper _mapper;

    public BroadcastHandler(NetworkGraph networkGraph, IMapper mapper)
    {
        _networkGraph = networkGraph;
        _mapper = mapper;
    }

    public async Task<(Vertex? localVertex, IEnumerable<BroadcastAdjacencyList>? broadcasts)> Handle(HandleBroadcastCommand request, CancellationToken cancellationToken)
    {
        if (request.BroadcastAdjacencyList == null)
        {
            return (null, null);
        }

        var vertex = _mapper.Map<Vertex>(request.BroadcastAdjacencyList);
        var (vertices, _) = await _networkGraph.AddAsync(vertex, cancellationToken);

        var broadcasts = vertices.Select(item => new BroadcastAdjacencyList
        {
            SignedData = item.SignedData,
            PublicKey = item.PublicKey
        });

        return (_networkGraph.LocalVertex, broadcasts);
    }
}
