/*
    Aenigma - Federated messaging system
    Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

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

using Enigma5.App.Common.Extensions;
using Enigma5.App.Data;
using Enigma5.App.Hubs.Sessions.Contracts;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.Security.Contracts;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class UpdateLocalAdjacencyHandler(
    NetworkGraph networkGraph,
    ICertificateManager certificateManager,
    ISessionManager sessionManager,
    ILogger<UpdateLocalAdjacencyHandler> logger)
: IRequestHandler<UpdateLocalAdjacencyCommand, CommandResult<VertexBroadcastRequestDto>>
{
    private readonly NetworkGraph _networkGraph = networkGraph;

    private readonly ISessionManager _sessionManager = sessionManager;

    private readonly ICertificateManager _certificateManager = certificateManager;

    private readonly ILogger<UpdateLocalAdjacencyHandler> _logger = logger;

    public async Task<CommandResult<VertexBroadcastRequestDto>> Handle(UpdateLocalAdjacencyCommand request, CancellationToken cancellationToken = default)
    {
        if(request.Addresses.Any(item => !item.IsValidAddress()))
        {
            return CommandResult.CreateResultFailure<VertexBroadcastRequestDto>();
        }

        Vertex localVertex;
        if (request.Add)
        {
            // Only addresses connected to this node right now may become neighbors.
            var connected = new List<string>();
            foreach (var address in request.Addresses)
            {
                if (await _sessionManager.TryGetConnectionIdAsync(address) is not null)
                {
                    connected.Add(address);
                }
            }

            // Re-sign first, so the broadcast carries a fresh lastUpdate even when no neighbor is new.
            await _networkGraph.GenerateLocalVertexAsync();
            localVertex = await _networkGraph.AddAdjacencyAsync(connected);
        }
        else
        {
            // Signs a new vertex only if one of the addresses actually was a neighbor.
            localVertex = await _networkGraph.RemoveAdjacencyAsync(request.Addresses);
        }

        if(localVertex.SignedData is null)
        {
            _logger.LogError("Local vertex has null signed data!");
            return CommandResult.CreateResultFailure<VertexBroadcastRequestDto>();
        }

        return CommandResult.CreateResultSuccess(new VertexBroadcastRequestDto(await _certificateManager.GetPublicKeyAsync(), localVertex.SignedData));
    }
}
