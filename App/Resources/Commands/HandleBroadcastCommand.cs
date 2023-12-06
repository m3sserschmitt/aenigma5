using Enigma5.App.Data;
using Enigma5.App.Models;
using MediatR;

namespace Enigma5.App.Resources.Commands;

public class HandleBroadcastCommand: IRequest<(Vertex? localVertex, IEnumerable<BroadcastAdjacencyList>? broadcasts)>
{
    public BroadcastAdjacencyList? BroadcastAdjacencyList { get; set; }
}
