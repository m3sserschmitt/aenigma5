using Enigma5.App.Data;
using Enigma5.App.Models;
using MediatR;

namespace Enigma5.App.Resources.Commands;

public class HandleBroadcastCommand(VertexBroadcastRequest broadcastAdjacencyList) : IRequest<(Vertex localVertex, IEnumerable<VertexBroadcastRequest> broadcasts)>
{
    public VertexBroadcastRequest BroadcastAdjacencyList { get; private set; } = broadcastAdjacencyList;
}
