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

using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Contracts;
using LinqKit;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class CleanupMessagesHandler(
    IDbWriter dbWriter
) : IRequestHandler<CleanupMessagesCommand, CommandResult<int>>
{
    private readonly IDbWriter _dbWriter = dbWriter;

    public async Task<CommandResult<int>> Handle(CleanupMessagesCommand request, CancellationToken cancellationToken = default)
    {
        var supTimestamp = (DateTimeOffset.UtcNow - request.TimeSpan).ToUnixTimeSeconds();
        var supDeliveredTimeSpan = (DateTimeOffset.UtcNow - request.DeliveredTimeSpan).ToUnixTimeSeconds();
        var predicate = PredicateBuilder.New<PendingMessage>(item =>
            (!item.Sent && supTimestamp > item.Timestamp) ||
            (item.Sent && item.SentTimestamp != null && supDeliveredTimeSpan > item.SentTimestamp));
        return CommandResult.CreateResultSuccess(await _dbWriter.RemoveMessagesAsync(predicate, cancellationToken));
    }
}
