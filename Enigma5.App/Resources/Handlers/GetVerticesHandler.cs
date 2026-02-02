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

using Enigma5.App.Models;
using Enigma5.App.Resources.Queries;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class GetVerticesHandler(Data.NetworkGraph graph) : IRequestHandler<GetVerticesQuery, CommandResult<List<VertexDto>>>
{
    private readonly Data.NetworkGraph _graph = graph;

    public async Task<CommandResult<List<VertexDto>>> Handle(GetVerticesQuery request, CancellationToken cancellationToken)
    => CommandResult.CreateResultSuccess((await _graph.GetVerticesAsync()).Select(item => new VertexDto
    {
        PublicKey = item.PublicKey,
        SignedData = item.SignedData,
        Neighborhood = new(
            item.Neighborhood.Address,
            item.Neighborhood.Hostname,
            item.Neighborhood.OnionService,
            item.Neighborhood.Neighbors,
            item.Neighborhood.LastUpdate
            )
    }).ToList());
}
