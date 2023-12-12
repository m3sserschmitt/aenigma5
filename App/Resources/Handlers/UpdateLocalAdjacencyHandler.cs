using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Security;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class UpdateLocalAdjacencyHandler
: IRequestHandler<UpdateLocalAdjacencyCommand, (Vertex localVertex, BroadcastAdjacencyList? broadcast)>
{
    private readonly NetworkGraph _networkGraph;

    private readonly CertificateManager _certificateManager;

    public UpdateLocalAdjacencyHandler(NetworkGraph networkGraph, CertificateManager certificateManager)
    {
        _networkGraph = networkGraph;
        _certificateManager = certificateManager;
    }

    public async Task<(Vertex localVertex, BroadcastAdjacencyList? broadcast)> Handle(UpdateLocalAdjacencyCommand request, CancellationToken cancellationToken = default)
    {
        var (newVertex, updated) = request.Add ?
        await _networkGraph.AddAdjacencyAsync(request.Address, cancellationToken)
        : await _networkGraph.RemoveAdjacencyAsync(request.Address, cancellationToken);

        if(!updated)
        {
            return (newVertex, null);
        }

        return (newVertex, new BroadcastAdjacencyList()
        {
            SignedData = newVertex.SignedData,
            PublicKey = _certificateManager.PublicKey
        });
    }
}
