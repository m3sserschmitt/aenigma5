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

public class CreateFileHandler(
    IConfiguration configuration,
    IDbWriter dbWriter,
    ILogger<CreateFileHandler> logger
) : IRequestHandler<CreateFileCommand, CommandResult<SharedDataDto>>
{
    private readonly IConfiguration _configuration = configuration;

    private readonly IDbWriter _dbWriter = dbWriter;

    private readonly ILogger _logger = logger;

    // Best effort: whatever cannot be removed here is removed by the cleanup job when the record expires.
    private async Task RemoveAsync(FileRecord record, string fullPath)
    {
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not delete a partly stored file.");
            return;
        }

        await _dbWriter.RemoveFileAsync(record, CancellationToken.None);
    }

    public async Task<CommandResult<SharedDataDto>> Handle(CreateFileCommand request, CancellationToken cancellationToken)
    {
        var webContentDirectory = _configuration.GetWebContentDirectory();
        if (webContentDirectory == null || request.File == null || request.File.Length == 0)
        {
            return CommandResult.CreateResultFailure<SharedDataDto>();
        }

        if (!string.IsNullOrEmpty(webContentDirectory) && !Directory.Exists(webContentDirectory))
        {
            Directory.CreateDirectory(webContentDirectory);
        }

        var record = new FileRecord
        {
            MaxAccessCount = request.MaxAccessCount
        };

        var fullPath = _configuration.GetWebContentFilePath(record.Tag);
        if (fullPath is null)
        {
            return CommandResult.CreateResultFailure<SharedDataDto>();
        }

        if (await _dbWriter.CreateFileAsync(record, cancellationToken) > 0)
        {
            try
            {
                using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
                await request.File.CopyToAsync(stream, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not store an uploaded file; its record is removed again.");
                await RemoveAsync(record, fullPath);
                return CommandResult.CreateResultFailure<SharedDataDto>();
            }

            return CommandResult.CreateResultSuccess(new SharedDataDto
            {
                Tag = record.Tag,
                ResourceUrl = _configuration.GetFileUrl(record.Tag),
                ValidUntil = _configuration.GetFileValidityDate(),
            });
        }
        else
        {
            return CommandResult.CreateResultFailure<SharedDataDto>();
        }
    }
}
