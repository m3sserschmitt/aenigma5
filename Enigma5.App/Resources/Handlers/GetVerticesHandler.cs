/*
    Aenigma - Federal messaging system
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

using Enigma5.App.Resources.Queries;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class GetVerticesHandler(Data.NetworkGraph graph) : IRequestHandler<GetVerticesQuery, CommandResult<List<Models.Vertex>>>
{
    private readonly Data.NetworkGraph _graph = graph;

    public Task<CommandResult<List<Models.Vertex>>> Handle(GetVerticesQuery request, CancellationToken cancellationToken)
    { 
        var vertices = _graph.NonLeafVertices.Select(item => new Models.Vertex {
            PublicKey = item.PublicKey,
            SignedData = item.SignedData,
            Neighborhood = new(item.Neighborhood.Address, item.Neighborhood.Hostname, item.Neighborhood.Neighbors)
        }).ToList();
        return Task.FromResult(CommandResult.CreateResultSuccess(vertices));
    }
}
