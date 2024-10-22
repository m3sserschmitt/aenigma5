using Enigma5.App.Data;
using Enigma5.App.Resources.Queries;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class GetVerticesHandler(NetworkGraph graph) : IRequestHandler<GetVerticesQuery, CommandResult<HashSet<Vertex>>>
{
    private readonly NetworkGraph _graph = graph;

    public Task<CommandResult<HashSet<Vertex>>> Handle(GetVerticesQuery request, CancellationToken cancellationToken)
    => Task.FromResult(CommandResult.CreateResultSuccess(_graph.NonLeafVertices));
}
