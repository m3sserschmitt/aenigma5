using AutoMapper;
using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class BroadcastHandler(NetworkGraph networkGraph, IMapper mapper)
: IRequestHandler<HandleBroadcastCommand, (Vertex localVertex, IEnumerable<BroadcastAdjacencyList> broadcasts)>
{
    private readonly NetworkGraph _networkGraph = networkGraph;

    private readonly IMapper _mapper = mapper;

    public async Task<(Vertex localVertex, IEnumerable<BroadcastAdjacencyList> broadcasts)> Handle(HandleBroadcastCommand request, CancellationToken cancellationToken = default)
    {
        var vertex = _mapper.Map<Vertex>(request.BroadcastAdjacencyList);
        var vertices = await _networkGraph.UpdateAsync(vertex, cancellationToken);

        var broadcasts = vertices.Select(item => new BroadcastAdjacencyList
        {
            SignedData = item.SignedData,
            PublicKey = item.PublicKey
        });

        return (_networkGraph.LocalVertex, broadcasts);
    }
}
