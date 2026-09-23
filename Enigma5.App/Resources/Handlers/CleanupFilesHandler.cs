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

public class CleanupFilesHandler(
    EnigmaDbContext context,
    IConfiguration configuration,
    IDbWriter dbWriter
) : IRequestHandler<CleanupFilesCommand, CommandResult<int>>
{
    private readonly IConfiguration _configuration = configuration;

    private readonly EnigmaDbContext _context = context;

    private readonly IDbWriter _dbWriter = dbWriter;

    public async Task<CommandResult<int>> Handle(CleanupFilesCommand request, CancellationToken cancellationToken)
    {
        var time = (DateTimeOffset.UtcNow - request.TimeSpan).ToUnixTimeSeconds();
        var webContentDirectory = _configuration.GetWebContentDirectory();
        var filesToBeRemoved = await _context.Files.Where(item => time > item.Timestamp).ToListAsync(cancellationToken: cancellationToken);
        var result = 0;
        foreach (var fileToBeRemoved in filesToBeRemoved)
        {
            if (!string.IsNullOrEmpty(webContentDirectory) && Directory.Exists(webContentDirectory))
            {
                var fullPath = Path.Combine(webContentDirectory, fileToBeRemoved.Tag);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            result += await _dbWriter.RemoveFileAsync(fileToBeRemoved, cancellationToken);
        }
        return CommandResult.CreateResultSuccess(result);
    }
}
