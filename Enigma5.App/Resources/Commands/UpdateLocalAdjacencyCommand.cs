using Enigma5.App.Data;
using Enigma5.App.Models;
using MediatR;

namespace Enigma5.App.Resources.Commands;

public class UpdateLocalAdjacencyCommand : IRequest<(Vertex localVertex, VertexBroadcast? broadcast)>
{
    public UpdateLocalAdjacencyCommand(string address, bool add)    
    {
        Address = address;
        Add = add;
    }
    
    public string Address { get; private set; }

    public bool Add { get; private set; }
}
