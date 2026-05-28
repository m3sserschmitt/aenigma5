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

using Enigma5.App.Common.Extensions;
using Enigma5.App.Common.Utils;
using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using LinqKit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class MarkMessagesAsDeliveredHandler(
    EnigmaDbContext dbContext,
    DbSingleThreadRunner dbSingleThreadRunner
) : IRequestHandler<MarkMessagesAsDeliveredCommand, CommandResult<int>>
{
    private readonly EnigmaDbContext _dbContext = dbContext;

    private readonly DbSingleThreadRunner _dbSingleThreadRunner = dbSingleThreadRunner;

    public async Task<CommandResult<int>> Handle(MarkMessagesAsDeliveredCommand request, CancellationToken cancellationToken)
    {
        if (!request.Destination.IsValidAddress())
        {
            return CommandResult.CreateResultFailure<int>();
        }

        var now = DateTimeOffset.Now;
        var utcTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var predicate = PredicateBuilder.New<PendingMessage>(item => item.Destination == request.Destination && !item.Sent);

        if (request.SupId != null)
        {
            predicate = predicate.And(item => item.Id <= request.SupId);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return CommandResult.CreateResultSuccess(await _dbSingleThreadRunner.RunAsync(() =>
        {
            return _dbContext.Messages
            .Where(predicate)
            .ExecuteUpdate(s =>
                s.SetProperty(m => m.Sent, true)
                .SetProperty(m => m.DateSent, now)
                .SetProperty(m => m.SentTimestamp, utcTimestamp));
        }));
    }
}
