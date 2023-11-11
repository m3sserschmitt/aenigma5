using Enigma5.App.Data;
using MediatR;

namespace Enigma5.App.Resources.Commands;

public class UpdateNetworkGraphCommand: IRequest<(IList<Vertex> vertices, Delta delta)>
{
    public Vertex? Vertex { get; set; }
}
