using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Security.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enigma5.App.Resources.Handlers;

public class UpdateLocalAdjacencyHandler(
    NetworkGraph networkGraph,
    ICertificateManager certificateManager,
    ILogger<UpdateLocalAdjacencyHandler> logger)
: IRequestHandler<UpdateLocalAdjacencyCommand, (Vertex localVertex, VertexBroadcastRequest? broadcast)>
{
    private readonly NetworkGraph _networkGraph = networkGraph;

    private readonly ICertificateManager _certificateManager = certificateManager;

    private readonly ILogger<UpdateLocalAdjacencyHandler> _logger = logger;

    public async Task<(Vertex localVertex, VertexBroadcastRequest? broadcast)> Handle(UpdateLocalAdjacencyCommand request, CancellationToken cancellationToken = default)
    {
        var (newLocalVertex, updated) = request.Add ?
        await _networkGraph.AddAdjacencyAsync(request.Address, cancellationToken)
        : await _networkGraph.RemoveAdjacencyAsync(request.Address, cancellationToken);

        if(!updated)
        {
            return (newLocalVertex, null);
        }

        if(newLocalVertex.SignedData is null)
        {
            _logger.LogError("Local vertex has null signed data!");
            return (newLocalVertex, null);
        }

        return (newLocalVertex, new VertexBroadcastRequest(_certificateManager.PublicKey, newLocalVertex.SignedData));
    }
}
