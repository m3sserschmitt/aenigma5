using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Security.Contracts;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class UpdateLocalAdjacencyHandler(NetworkGraph networkGraph, ICertificateManager certificateManager)
: IRequestHandler<UpdateLocalAdjacencyCommand, (Vertex localVertex, VertexBroadcastRequest? broadcast)>
{
    private readonly NetworkGraph _networkGraph = networkGraph;

    private readonly ICertificateManager _certificateManager = certificateManager;

    public async Task<(Vertex localVertex, VertexBroadcastRequest? broadcast)> Handle(UpdateLocalAdjacencyCommand request, CancellationToken cancellationToken = default)
    {
        var (newVertex, updated) = request.Add ?
        await _networkGraph.AddAdjacencyAsync(request.Address, cancellationToken)
        : await _networkGraph.RemoveAdjacencyAsync(request.Address, cancellationToken);

        if(!updated)
        {
            return (newVertex, null);
        }

        if(newVertex.SignedData is null)
        {
            // TODO: This should not happen! Log this
            return (newVertex, null);
        }

        return (newVertex, new VertexBroadcastRequest(_certificateManager.PublicKey, newVertex.SignedData));
    }
}
