using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Security;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class UpdateLocalAdjacencyHandler
: IRequestHandler<UpdateLocalAdjacencyCommand, (Vertex localVertex, BroadcastAdjacencyList broadcast)>
{
    private readonly NetworkGraph _networkGraph;

    private readonly CertificateManager _certificateManager;

    public UpdateLocalAdjacencyHandler(NetworkGraph networkGraph, CertificateManager certificateManager)
    {
        _networkGraph = networkGraph;
        _certificateManager = certificateManager;
    }

    public async Task<(Vertex localVertex, BroadcastAdjacencyList broadcast)> Handle(UpdateLocalAdjacencyCommand request, CancellationToken cancellationToken)
    {
        var newVertex = await _networkGraph.AddAsync(request.Address, cancellationToken);

        return (newVertex, new BroadcastAdjacencyList()
        {
            SignedData = newVertex.SignedData,
            PublicKey = _certificateManager.PublicKey
        });
    }
}
