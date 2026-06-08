/*
    Aenigma - Federated messaging system
    Copyright © 2024-2026 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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
using Enigma5.App.Common.Utils;
using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class CreatePendingMessageHandler(
    EnigmaDbContext context,
    DbSingleThreadRunner dbSingleThreadRunner
) : IRequestHandler<CreatePendingMessageCommand, CommandResult<PendingMessageDto>>
{
    private readonly EnigmaDbContext _context = context;

    private readonly DbSingleThreadRunner _dbSingleThreadRunner = dbSingleThreadRunner;

    public async Task<CommandResult<PendingMessageDto>> Handle(CreatePendingMessageCommand request, CancellationToken cancellationToken)
    {
        if (!request.Destination.IsValidAddress() || !request.Content.IsValidBase64())
        {
            return CommandResult.CreateResultFailure<PendingMessageDto>();
        }

        var pendingMessage = new PendingMessage
        {
            Destination = request.Destination,
            Content = request.Content
        };

        if (request.Uuid != null)
        {
            var existingEntry = await _context.Messages.FirstOrDefaultAsync(item => item.Uuid == request.Uuid, cancellationToken: cancellationToken);
            if (existingEntry != null)
            {
                return CommandResult.CreateResultFailure(new PendingMessageDto
                {
                    Id = existingEntry.Id,
                    Uuid = existingEntry.Uuid,
                    Sent = existingEntry.Sent,
                    Destination = existingEntry.Destination,
                    Content = existingEntry.Content,
                    DateReceived = existingEntry.DateCreated
                });
            }
            pendingMessage.Uuid = request.Uuid;
        }
        
        cancellationToken.ThrowIfCancellationRequested();
        await _dbSingleThreadRunner.RunAsync(() =>
        {
            _context.Add(pendingMessage);
            return _context.SaveChanges();
        });

        return CommandResult.CreateResultSuccess(new PendingMessageDto
        {
            Id = pendingMessage.Id,
            Uuid = pendingMessage.Uuid,
            Sent = pendingMessage.Sent,
            Destination = pendingMessage.Destination,
            Content = pendingMessage.Content,
            DateReceived = pendingMessage.DateCreated
        });
    }
}
