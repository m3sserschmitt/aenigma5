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
using Enigma5.Crypto.Extensions;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class CreatePendingMessageHandler(EnigmaDbContext context) : IRequestHandler<CreatePendingMessageCommand, CommandResult<PendingMessage>>
{
    private readonly EnigmaDbContext _context = context;

    public async Task<CommandResult<PendingMessage>> Handle(CreatePendingMessageCommand request, CancellationToken cancellationToken)
    {
        if(!request.Destination.IsValidAddress() || !request.Content.IsValidBase64())
        {
            return CommandResult.CreateResultFailure<PendingMessage>();
        }

        var pendingMessage = new PendingMessage
        {
            Destination = request.Destination,
            Content = request.Content
        };
        await _context.AddAsync(pendingMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return CommandResult.CreateResultSuccess(pendingMessage);
    }
}
