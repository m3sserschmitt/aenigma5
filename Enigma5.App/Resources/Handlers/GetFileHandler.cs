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

public class GetFileHandler(Data.EnigmaDbContext context) : IRequestHandler<GetFileQuery, CommandResult<Models.SharedData>>
{
    private readonly Data.EnigmaDbContext _context = context;

    public async Task<CommandResult<Models.SharedData>> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        var file = await _context.Files.FirstOrDefaultAsync(item => item.Tag == request.Tag, cancellationToken: cancellationToken);

        if (file is null)
        {
            return CommandResult.CreateResultSuccess<Models.SharedData>();
        }

        file.AccessCount++;
        if (file.AccessCount >= file.MaxAccessCount)
        {
            _context.Files.Remove(file);
        }
        else
        {
            _context.Files.Update(file);
        }

        await _context.SaveChangesAsync(cancellationToken);
        
        return CommandResult.CreateResultSuccess(new Models.SharedData
        {
            Tag = file.Tag,
            BinData = file.Data
        });
    }
}
