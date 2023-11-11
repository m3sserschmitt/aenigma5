using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using MediatR;

namespace App;

public class UpdateNetworkGraphHandler
: IRequestHandler<UpdateNetworkGraphCommand, (IList<Vertex> vertices, Delta delta)>
{
    private readonly NetworkGraph _networkGraph;

    public UpdateNetworkGraphHandler(NetworkGraph networkGraph)
    {
        _networkGraph = networkGraph;
    }

    public async Task<(IList<Vertex> vertices, Delta delta)> Handle(UpdateNetworkGraphCommand request, CancellationToken cancellationToken)
    {
        if (request.Vertex != null)
        {
            return await _networkGraph.AddAsync(request.Vertex, cancellationToken);
        }

        return (new List<Vertex>(), new());
    }
}
