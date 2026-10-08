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

public class GetServerInfoHandlerTests
{
    [Fact]
    public async Task The_key_the_address_the_names_and_the_graph_version_are_returned()
    {
        await using var node = await TestNode.StartAsync(("OnionService", "http://example.onion"));

        var result = await node.Send(new GetServerInfoQuery());

        Assert.True(result.Success);
        Assert.Equal(TestKeys.PublicKey1, result.Value!.PublicKey);
        Assert.Equal(TestKeys.Address1, result.Value.Address);
        Assert.Equal("http://node.example", result.Value.Hostname);
        Assert.Equal("http://example.onion", result.Value.OnionService);
        Assert.Equal(await node.Get<NetworkGraph>().GetGraphHashAsync(), result.Value.GraphVersion);
        Assert.NotNull(result.Value.GraphVersion);
    }

    [Fact]
    public async Task A_locked_node_reports_no_graph_version()
    {
        await using var node = await TestNode.StartAsync();
        node.Key.Locked = true;
        await node.Get<NetworkGraph>().GenerateLocalVertexAsync();

        var result = await node.Send(new GetServerInfoQuery());

        Assert.True(result.Success);
        Assert.Null(result.Value!.GraphVersion);
        Assert.Equal(TestKeys.Address1, result.Value.Address);
    }
}
