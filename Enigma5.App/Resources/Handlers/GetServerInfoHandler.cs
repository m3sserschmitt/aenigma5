using System.Text;
using System.Text.Json;
using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Queries;
using Enigma5.Crypto;
using Enigma5.Security.Contracts;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class GetServerInfoHandler(
    NetworkGraph graph,
    ICertificateManager certificateManager) : IRequestHandler<GetServerInfoQuery, CommandResult<ServerInfo>>
{
    private readonly NetworkGraph _graph = graph;

    private readonly ICertificateManager _certificateManager = certificateManager;

    public Task<CommandResult<ServerInfo>> Handle(GetServerInfoQuery request, CancellationToken cancellationToken)
    {
        var serializedGraph = JsonSerializer.Serialize(_graph.Vertices);
        var graphVersion = HashProvider.Sha256Hex(Encoding.UTF8.GetBytes(serializedGraph));

        return Task.FromResult(CommandResult.CreateResultSuccess(
            new ServerInfo
            {
                PublicKey = _certificateManager.PublicKey,
                Address = _certificateManager.Address,
                GraphVersion = graphVersion
            }));
    }
}
