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
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class CleanupMessagesHandler(EnigmaDbContext context)
: IRequestHandler<CleanupMessagesCommand, CommandResult<int>>
{
    private readonly EnigmaDbContext _context = context;

    public async Task<CommandResult<int>> Handle(CleanupMessagesCommand request, CancellationToken cancellationToken = default)
    {
        // TODO: refactor this query
        var time = DateTimeOffset.Now - request.TimeSpan;
        var deliveredTime = DateTime.Now - request.DeliveredTimeSpan;
        var messages = await _context.Messages.ToListAsync(cancellationToken: cancellationToken);
        _context.Messages.RemoveRange(messages.Where(item =>
        (!item.Sent && time > item.DateReceived) || (item.Sent && item.DateSent != null && deliveredTime > item.DateSent)));
        return CommandResult.CreateResultSuccess(await _context.SaveChangesAsync(cancellationToken));
    }
}
