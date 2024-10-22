using Enigma5.App.Models;
using Enigma5.App.Resources.Handlers;
using MediatR;

namespace Enigma5.App.Resources.Queries;

public class GetServerInfoQuery : IRequest<CommandResult<ServerInfo>> { }
