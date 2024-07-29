using Enigma5.App.Data;
using Enigma5.App.Models;
using MediatR;

namespace Enigma5.App.Resources.Commands;

public class UpdateLocalAdjacencyCommand(string address, bool add) : IRequest<(Vertex localVertex, VertexBroadcastRequest? broadcast)>
{
    public string Address { get; private set; } = address;

    public bool Add { get; private set; } = add;
}
