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

using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Queries;
using LinqKit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class GetPendingMessagesByDestinationHandler(EnigmaDbContext context)
: IRequestHandler<GetPendingMessagesByDestinationQuery, CommandResult<List<PendingMessageDto>>>
{
    private readonly EnigmaDbContext _context = context;

    public async Task<CommandResult<List<PendingMessageDto>>> Handle(GetPendingMessagesByDestinationQuery request, CancellationToken cancellationToken)
    {
        var predicate = PredicateBuilder.New<PendingMessage>(item => item.Destination == request.Destination && !item.Sent);
        
        if (request.InfId != null)
        {
            predicate = predicate.And(item => item.Id > request.InfId);
        }

        return CommandResult.CreateResultSuccess(await _context.Messages
        .Where(predicate)
        .OrderBy(item => item.Id)
        .Take(request.PageSize).Select(item => new PendingMessageDto
        {
            Id = item.Id,
            Uuid = item.Uuid,
            Destination = item.Destination,
            Content = item.Content,
            DateReceived = item.DateCreated,
            Sent = item.Sent
        }
        ).ToListAsync(cancellationToken));
    }
}
