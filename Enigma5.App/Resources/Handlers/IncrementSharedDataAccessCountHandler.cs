/*
    Aenigma - Onion Routing based messaging application
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

public class IncrementSharedDataAccessCountHandler(EnigmaDbContext context)
: IRequestHandler<IncrementSharedDataAccessCountCommand, CommandResult<SharedData>>
{
    private readonly EnigmaDbContext _context = context;

    public async Task<CommandResult<SharedData>> Handle(IncrementSharedDataAccessCountCommand request, CancellationToken cancellationToken)
    {
        var sharedData = await _context.SharedData.FirstOrDefaultAsync(
            item => item.Tag == request.Tag,
            cancellationToken: cancellationToken);

        if (sharedData is not null)
        {
            sharedData.AccessCount += 1;
            _context.Update(sharedData);
            await _context.SaveChangesAsync(cancellationToken);
            return CommandResult.CreateResultSuccess(sharedData);
        }

        return CommandResult.CreateResultSuccess<SharedData>();
    }
}
