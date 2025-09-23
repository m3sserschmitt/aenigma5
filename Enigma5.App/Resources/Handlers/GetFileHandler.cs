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
using Enigma5.App.Resources.Queries;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Resources.Handlers;

public class GetFileHandler(IConfiguration configuration) : IRequestHandler<GetFileQuery, CommandResult<Models.SharedData>>
{
    private readonly IConfiguration _configuration = configuration;

    public Task<CommandResult<Models.SharedData>> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        var webContentDirectory = _configuration.GetWebContentDirectory();
        if (string.IsNullOrEmpty(webContentDirectory) || !Directory.Exists(webContentDirectory))
        {
            return Task.FromResult(CommandResult.CreateResultSuccess<Models.SharedData>());
        }
        var fullPath = Path.Combine(webContentDirectory, request.Tag);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult(CommandResult.CreateResultSuccess<Models.SharedData>());
        }

        return Task.FromResult(CommandResult.CreateResultSuccess(new Models.SharedData
        {
            Tag = request.Tag,
            File = new FileStream(fullPath, FileMode.Open, FileAccess.Read)
        }));
    }
}
