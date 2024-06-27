using Enigma5.App.Data;
using Enigma5.App.Models;
using MediatR;

namespace Enigma5.App.Resources.Commands;

public class HandleBroadcastCommand: IRequest<(Vertex localVertex, IEnumerable<BroadcastAdjacencyList> broadcasts)>
{
    public HandleBroadcastCommand(BroadcastAdjacencyList broadcastAdjacencyList)
    {
        BroadcastAdjacencyList = broadcastAdjacencyList;
    }
    
    public BroadcastAdjacencyList BroadcastAdjacencyList { get; private set; }
}
