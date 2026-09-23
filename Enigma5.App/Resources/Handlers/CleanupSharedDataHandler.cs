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
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Contracts;
using LinqKit;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class CleanupSharedDataHandler(
    IDbWriter dbWriter
) : IRequestHandler<CleanupSharedDataCommand, CommandResult<int>>
{
    private readonly IDbWriter _dbWriter = dbWriter;

    public async Task<CommandResult<int>> Handle(CleanupSharedDataCommand request, CancellationToken cancellationToken = default)
    {
        var supTimestamp = (DateTimeOffset.UtcNow - request.TimeSpan).ToUnixTimeSeconds();
        var predicate = PredicateBuilder.New<SharedData>(item => supTimestamp > item.Timestamp);
        return CommandResult.CreateResultSuccess(await _dbWriter.RemoveSharedDataAsync(predicate, cancellationToken));
    }
}
