using Enigma5.App.Models;
using MediatR;

namespace Enigma5.App.Resources.Commands;

public class SynchronizeConnectionsCommand: IRequest
{
    public BroadcastAdjacencyList? BroadcastAdjacencyList { get; set; }
}
