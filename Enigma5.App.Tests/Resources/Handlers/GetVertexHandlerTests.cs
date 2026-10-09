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

public class GetVertexHandlerTests
{
    [Fact]
    public async Task The_vertex_of_a_known_address_is_returned()
    {
        await using var node = await TestNode.StartAsync();

        var result = await node.Send(new GetVertexQuery(TestKeys.Address1));

        Assert.True(result.Success);
        Assert.Equal(TestKeys.PublicKey1, result.Value!.PublicKey);
        Assert.NotNull(result.Value.SignedData);
        Assert.Equal(TestKeys.Address1, result.Value.Neighborhood!.Address);
        Assert.Equal("http://node.example", result.Value.Neighborhood.Hostname);
    }

    [Fact]
    public async Task An_unknown_address_gives_success_without_a_value()
    {
        await using var node = await TestNode.StartAsync();

        var result = await node.Send(new GetVertexQuery(TestKeys.Address3));

        Assert.True(result.Success);
        Assert.Null(result.Value);
    }
}
