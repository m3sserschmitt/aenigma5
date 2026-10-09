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
using Enigma5.App.Resources.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class GetFileHandler(EnigmaDbContext context, IConfiguration configuration) : IRequestHandler<GetFileQuery, CommandResult<SharedDataDto>>
{
    private readonly EnigmaDbContext _context = context;

    private readonly IConfiguration _configuration = configuration;

    public async Task<CommandResult<SharedDataDto>> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        // Not found is a successful lookup without a value; HTTP callers turn it into 404.
        // A file needs both its record and its content on disk.
        var fullPath = _configuration.GetWebContentFilePath(request.Tag);
        if (fullPath is null || !File.Exists(fullPath))
        {
            return CommandResult.CreateResultSuccess<SharedDataDto>(null);
        }

        var fileRecord = await _context.Files.FirstOrDefaultAsync(item => item.Tag == request.Tag, cancellationToken);
        if (fileRecord is null)
        {
            return CommandResult.CreateResultSuccess<SharedDataDto>(null);
        }

        return CommandResult.CreateResultSuccess(new SharedDataDto
        {
            Tag = fileRecord.Tag,
            File = new FileStream(fullPath, FileMode.Open, FileAccess.Read)
        });
    }
}
