using Enigma5.App.Data;
using Enigma5.App.Resources.Handlers;
using MediatR;

namespace Enigma5.App.Resources.Queries;

public class GetVerticesQuery : IRequest<CommandResult<HashSet<Vertex>>> { }
