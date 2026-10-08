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

using Enigma5.App.Data;
using Enigma5.App.Data.Extensions;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Queries;
using Enigma5.App.UI;
using Enigma5.Tests.Base;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Tests.Resources.Handlers;

public class GetVerticesHandlerTests
{
    [Fact]
    public async Task All_vertices_of_the_graph_are_returned_with_the_nodes_own()
    {
        await using var node = await TestNode.StartAsync();
        var other = await Vertex.Factory.CreateAsync(FixedKeyCertificateManager.Key2(), [TestKeys.Address3]);
        await node.Send(new HandleBroadcastCommand(other.ToVertexBroadcast()));

        var result = await node.Send(new GetVerticesQuery());

        Assert.True(result.Success);
        Assert.Equal([TestKeys.Address2, TestKeys.Address1], result.Value!.Select(vertex => vertex.Neighborhood!.Address).Order());
        Assert.Equal([TestKeys.Address3], result.Value!.Single(vertex => vertex.Neighborhood!.Address == TestKeys.Address2).Neighborhood!.Neighbors);
    }
}
