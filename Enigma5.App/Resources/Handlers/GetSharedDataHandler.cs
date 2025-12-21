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

using Enigma5.App.Resources.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class GetSharedDataHandler(Data.EnigmaDbContext context) : IRequestHandler<GetSharedDataQuery, CommandResult<Models.SharedDataDto>>
{
    private readonly Data.EnigmaDbContext _context = context;

    public async Task<CommandResult<Models.SharedDataDto>> Handle(GetSharedDataQuery request, CancellationToken cancellationToken)
    {
        var sharedData = await _context.SharedData.FirstOrDefaultAsync(item => item.Tag == request.Tag, cancellationToken: cancellationToken);
        return sharedData is null ? CommandResult.CreateResultSuccess<Models.SharedDataDto>() : CommandResult.CreateResultSuccess(new Models.SharedDataDto
        {
            Tag = sharedData.Tag,
            Data = sharedData.Data,
            PublicKey = sharedData.PublicKey
        });
    }
}
