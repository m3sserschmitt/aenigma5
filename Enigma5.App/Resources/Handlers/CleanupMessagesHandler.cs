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
using Enigma5.App.Resources.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class CleanupMessagesHandler(EnigmaDbContext context)
: IRequestHandler<CleanupMessagesCommand>
{
    private readonly EnigmaDbContext _context = context;

    public async Task Handle(CleanupMessagesCommand command, CancellationToken cancellationToken)
    {
        // TODO: refactor this query
        var time = DateTimeOffset.Now - command.TimeSpan;
        var messages = await _context.Messages.ToListAsync(cancellationToken: cancellationToken);
        _context.Messages.RemoveRange(messages.Where(item =>
            time > item.DateReceived ||
            (command.RemoveDelivered && item.Sent)));
        await _context.SaveChangesAsync(cancellationToken);
    }
}
