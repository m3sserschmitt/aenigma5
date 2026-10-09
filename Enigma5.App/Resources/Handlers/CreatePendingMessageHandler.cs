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

public class CreatePendingMessageHandler(
    IDbWriter dbWriter
) : IRequestHandler<CreatePendingMessageCommand, CommandResult<PendingMessageDto>>
{
    private readonly IDbWriter _dbWriter = dbWriter;

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
            pendingMessage.Uuid = request.Uuid;
        }

        // A given uuid is checked by the writer: if a message with it is already stored, that one is returned.
        var storedMessage = await _dbWriter.CreatePendingMessageAsync(pendingMessage, request.Uuid != null, cancellationToken);
        if (storedMessage is null)
        {
            return CommandResult.CreateResultFailure<PendingMessageDto>();
        }

        var storedMessageDto = new PendingMessageDto
        {
            Id = storedMessage.Id,
            Uuid = storedMessage.Uuid,
            Sent = storedMessage.Sent,
            Destination = storedMessage.Destination,
            Content = storedMessage.Content,
            DateReceived = storedMessage.DateCreated
        };

        // An exception to the rule that a failure carries no value: for a uuid that is already stored the result
        // is a failure with the stored message, so the hub can accept the request without delivering it again.
        return ReferenceEquals(storedMessage, pendingMessage) ?
        CommandResult.CreateResultSuccess(storedMessageDto) : CommandResult.CreateResultFailure(storedMessageDto);
    }
}
