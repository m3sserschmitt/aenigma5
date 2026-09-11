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
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class IncrementFileAccessCountHandler(
    EnigmaDbContext context,
    IConfiguration configuration,
    IDbWriter dbWriter
) : IRequestHandler<IncrementFileAccessCountCommand, CommandResult<int>>
{
    private readonly IConfiguration _configuration = configuration;

    private readonly EnigmaDbContext _context = context;

    private readonly IDbWriter _dbWriter = dbWriter;

    private void RemoveFile(FileRecord fileRecord, IncrementFileAccessCountCommand request)
    {
        var webContentDirectory = _configuration.GetWebContentDirectory();
        if (fileRecord.AccessCount >= fileRecord.MaxAccessCount)
        {
            if (!string.IsNullOrWhiteSpace(webContentDirectory))
            {
                var fullPath = Path.Combine(webContentDirectory, request.Tag);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }
    }

    public async Task<CommandResult<int>> Handle(IncrementFileAccessCountCommand request, CancellationToken cancellationToken)
    {
        var fileRecord = await _context.Files.FirstOrDefaultAsync(
            item => item.Tag == request.Tag,
            cancellationToken: cancellationToken);

        if (fileRecord is not null)
        {
            var result = await _dbWriter.IncrementFileAccessCountAsync(fileRecord, cancellationToken);
            RemoveFile(fileRecord, request);
            return result > 0 ? CommandResult.CreateResultSuccess(result) : CommandResult.CreateResultFailure<int>();
        }

        return CommandResult.CreateResultFailure<int>();
    }
}
