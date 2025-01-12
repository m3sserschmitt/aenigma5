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

namespace Enigma5.App.Resources.Handlers;

public class GetVertexHandler(Data.NetworkGraph graph)
: IRequestHandler<GetVertexQuery, CommandResult<Models.Vertex>>
{
    private readonly Data.NetworkGraph _graph = graph;

    public async Task<CommandResult<Models.Vertex>> Handle(GetVertexQuery request, CancellationToken cancellationToken)
    {
        var vertex = await _graph.GetVertexAsync(request.Address, cancellationToken);

        if(vertex is null)
        {
            return CommandResult.CreateResultSuccess<Models.Vertex>();
        }

        return CommandResult.CreateResultSuccess(new Models.Vertex {
            PublicKey = vertex.PublicKey,
            SignedData = vertex.SignedData,
            Neighborhood = new(vertex.Neighborhood.Address, vertex.Neighborhood.Hostname, vertex.Neighborhood.Neighbors)
        });
    }
}
