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
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class CreatePendingMessageHandler(EnigmaDbContext context) : IRequestHandler<CreatePendingMessageCommand, CommandResult<PendingMessage>>
{
    private readonly EnigmaDbContext _context = context;

    async Task<CommandResult<PendingMessage>> IRequestHandler<CreatePendingMessageCommand, CommandResult<PendingMessage>>.Handle(CreatePendingMessageCommand request, CancellationToken cancellationToken)
    {
        var pendingMessage = new PendingMessage(request.Destination, request.Content, false);
        await _context.AddAsync(pendingMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return CommandResult.CreateResultSuccess(pendingMessage);
    }
}
