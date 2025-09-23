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

using Enigma5.App.Common.Constants;
using Enigma5.App.Common.Extensions;
using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Resources.Handlers;

public class CreateFileHandler(EnigmaDbContext context, IConfiguration configuration)
    : IRequestHandler<CreateFileCommand, CommandResult<Models.SharedData>>
{
    private readonly EnigmaDbContext _context = context;

    private readonly IConfiguration _configuration = configuration;

    public async Task<CommandResult<Models.SharedData>> Handle(CreateFileCommand request, CancellationToken cancellationToken)
    {
        var webContentDirectory = _configuration.GetWebContentDirectory();
        if (webContentDirectory == null || request.File == null || request.File.Length == 0)
        {
            return CommandResult.CreateResultFailure<Models.SharedData>();
        }

        if (!string.IsNullOrEmpty(webContentDirectory) && !Directory.Exists(webContentDirectory))
        {
            Directory.CreateDirectory(webContentDirectory);
        }

        var record = new FileRecord
        {
            Data = [0],
            MaxAccessCount = request.MaxAccessCount
        };

        _context.Files.Add(record);
        await _context.SaveChangesAsync(cancellationToken);

        string fullPath = Path.Combine(webContentDirectory, record.Tag);
        using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await request.File.CopyToAsync(stream, cancellationToken);

        var hostname = _configuration.GetHostname();
        return CommandResult.CreateResultSuccess(new Models.SharedData
        {
            Tag = record.Tag,
            ResourceUrl = hostname is not null ? $"{hostname}/{Endpoints.FileEndpoint}?Tag={record.Tag}" : null,
            ValidUntil = DateTimeOffset.Now + _configuration.GetFilesRetentionPeriod(),
        });
    }
}
