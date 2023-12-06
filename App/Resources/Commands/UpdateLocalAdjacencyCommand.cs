using Enigma5.App.Data;
using Enigma5.App.Models;
using MediatR;

namespace Enigma5.App.Resources.Commands;

public class UpdateLocalAdjacencyCommand : IRequest<(Vertex? localVertex, BroadcastAdjacencyList? broadcast)>
{
    public string? Address { get; set; }

    public bool Add { get; set; }
}
