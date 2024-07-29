using Enigma5.App.Data;
using Enigma5.App.Data.Extensions;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class BroadcastHandler(NetworkGraph networkGraph)
: IRequestHandler<HandleBroadcastCommand, (Vertex localVertex, IEnumerable<VertexBroadcastRequest> broadcasts)>
{
    private readonly NetworkGraph _networkGraph = networkGraph;

    public async Task<(Vertex localVertex, IEnumerable<VertexBroadcastRequest> broadcasts)> Handle(HandleBroadcastCommand request, CancellationToken cancellationToken = default)
    {
        var vertex = request.BroadcastAdjacencyList.ToVertex();
        var vertices = await _networkGraph.UpdateAsync(vertex, cancellationToken);

        var broadcasts = vertices.Select(item => item.ToVertexBroadcast());

        return (_networkGraph.LocalVertex, broadcasts);
    }
}
