using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class BroadcastHandler(NetworkGraph networkGraph)
: IRequestHandler<HandleBroadcastCommand, (Vertex localVertex, IEnumerable<VertexBroadcast> broadcasts)>
{
    private readonly NetworkGraph _networkGraph = networkGraph;

    public async Task<(Vertex localVertex, IEnumerable<VertexBroadcast> broadcasts)> Handle(HandleBroadcastCommand request, CancellationToken cancellationToken = default)
    {
        var vertex = Vertex.FromBroadcast(request.BroadcastAdjacencyList);
        var vertices = await _networkGraph.UpdateAsync(vertex, cancellationToken);

        var broadcasts = vertices.Select(Vertex.ToBroadcast);

        return (_networkGraph.LocalVertex, broadcasts);
    }
}
