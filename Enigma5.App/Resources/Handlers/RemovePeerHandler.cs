/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class RemovePeerHandler(IMediator mediator, EnigmaDbContext dbContext) : IRequestHandler<RemovePeerCommand, CommandResult<bool>>
{
    private readonly EnigmaDbContext _dbContext = dbContext;

    private readonly IMediator _mediator = mediator;

    public async Task<CommandResult<bool>> Handle(RemovePeerCommand request, CancellationToken cancellationToken = default)
    {
        var peer = await _dbContext.Peers.FindAsync([request.Id], cancellationToken: cancellationToken);
        if (peer == null)
        {
            return CommandResult.CreateResultFailure(false);
        }
        _dbContext.Remove(peer);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _mediator.Send(new InvokeNetworkBridgeCommand(), cancellationToken);
        return CommandResult.CreateResultSuccess(true);
    }
}
