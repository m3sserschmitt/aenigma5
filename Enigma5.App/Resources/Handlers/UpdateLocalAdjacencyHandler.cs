/*
    Aenigma - Federal messaging system
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
using Enigma5.Crypto.Extensions;
using Enigma5.Security.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enigma5.App.Resources.Handlers;

public class UpdateLocalAdjacencyHandler(
    NetworkGraph networkGraph,
    ICertificateManager certificateManager,
    ILogger<UpdateLocalAdjacencyHandler> logger)
: IRequestHandler<UpdateLocalAdjacencyCommand, CommandResult<VertexBroadcastRequest>>
{
    private readonly NetworkGraph _networkGraph = networkGraph;

    private readonly ICertificateManager _certificateManager = certificateManager;

    private readonly ILogger<UpdateLocalAdjacencyHandler> _logger = logger;

    public async Task<CommandResult<VertexBroadcastRequest>> Handle(UpdateLocalAdjacencyCommand request, CancellationToken cancellationToken = default)
    {
        if(request.Addresses.Any(item => !item.IsValidAddress()))
        {
            return CommandResult.CreateResultFailure<VertexBroadcastRequest>();
        }

        var (newLocalVertex, updated) = request.Add ?
        await _networkGraph.AddAdjacencyAsync(request.Addresses, cancellationToken)
        : await _networkGraph.RemoveAdjacencyAsync(request.Addresses, cancellationToken);

        if(!updated)
        {
            return CommandResult.CreateResultSuccess<VertexBroadcastRequest>();
        }

        if(newLocalVertex.SignedData is null)
        {
            _logger.LogError("Local vertex has null signed data!");
            return CommandResult.CreateResultFailure<VertexBroadcastRequest>();
        }

        return CommandResult.CreateResultSuccess(new VertexBroadcastRequest(_certificateManager.PublicKey, newLocalVertex.SignedData));
    }
}
