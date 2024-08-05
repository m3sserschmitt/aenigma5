using Enigma5.App.Data;
using Enigma5.App.Models;
using MediatR;

namespace Enigma5.App.Resources.Commands;

public class UpdateLocalAdjacencyCommand(List<string> addresses, bool add) : IRequest<(Vertex localVertex, VertexBroadcastRequest? broadcast)>
{
    public List<string> Address { get; private set; } = addresses;

    public bool Add { get; private set; } = add;
}
