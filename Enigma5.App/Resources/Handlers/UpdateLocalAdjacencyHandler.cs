/*
    Aenigma - Onion Routing based messaging application
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.Security.Contracts;
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
