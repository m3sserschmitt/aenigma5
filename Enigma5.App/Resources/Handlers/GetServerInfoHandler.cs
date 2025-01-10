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

using System.Text;
using System.Text.Json;
using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Queries;
using Enigma5.Crypto;
using Enigma5.Security.Contracts;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class GetServerInfoHandler(
    NetworkGraph graph,
    ICertificateManager certificateManager) : IRequestHandler<GetServerInfoQuery, CommandResult<ServerInfo>>
{
    private readonly NetworkGraph _graph = graph;

    private readonly ICertificateManager _certificateManager = certificateManager;

    public Task<CommandResult<ServerInfo>> Handle(GetServerInfoQuery request, CancellationToken cancellationToken)
    {
        var serializedGraph = JsonSerializer.Serialize(_graph.Vertices);
        var graphVersion = HashProvider.Sha256Hex(Encoding.UTF8.GetBytes(serializedGraph));

        return Task.FromResult(CommandResult.CreateResultSuccess(
            new ServerInfo
            {
                PublicKey = _certificateManager.PublicKey,
                Address = _certificateManager.Address,
                GraphVersion = graphVersion
            }));
    }
}
