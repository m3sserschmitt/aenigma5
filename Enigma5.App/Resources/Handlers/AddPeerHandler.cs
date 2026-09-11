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
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Contracts;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class AddPeerHandler(
    IMediator mediator,
    IDbWriter dbWriter
) : IRequestHandler<AddPeerCommand, CommandResult<PeerDto>>
{
    private readonly IMediator _mediator = mediator;

    private readonly IDbWriter _dbWriter = dbWriter;

    public async Task<CommandResult<PeerDto>> Handle(AddPeerCommand request, CancellationToken cancellationToken = default)
    {
        if (request.Address.IsValidAddress() && Uri.TryCreate(request.Host, UriKind.Absolute, out var parsedUri))
        {
            var peer = new Peer
            {
                Host = parsedUri.ToString(),
                Address = request.Address
            };

            var result = await _dbWriter.CreatePeerAsync(peer, cancellationToken);
            await _mediator.Send(new InvokeNetworkBridgeCommand(), cancellationToken);

            return result > 0 ? CommandResult.CreateResultSuccess(new PeerDto
            {
                Id = peer.Id,
                Host = peer.Host,
                Address = peer.Address
            }) : CommandResult.CreateResultFailure<PeerDto>();
        }
        return CommandResult.CreateResultFailure<PeerDto>();
    }
}
