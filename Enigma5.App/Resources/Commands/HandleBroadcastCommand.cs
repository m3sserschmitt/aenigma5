using Enigma5.App.Data;
using Enigma5.App.Models;
using MediatR;

namespace Enigma5.App.Resources.Commands;

public class HandleBroadcastCommand: IRequest<(Vertex localVertex, IEnumerable<VertexBroadcast> broadcasts)>
{
    public HandleBroadcastCommand(VertexBroadcast broadcastAdjacencyList)
    {
        BroadcastAdjacencyList = broadcastAdjacencyList;
    }
    
    public VertexBroadcast BroadcastAdjacencyList { get; private set; }
}
