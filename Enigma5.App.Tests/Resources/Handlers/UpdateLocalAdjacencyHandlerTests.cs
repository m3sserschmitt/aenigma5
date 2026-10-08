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

public class UpdateLocalAdjacencyHandlerTests
{
    [Fact]
    public async Task An_address_with_a_session_is_added_and_the_new_vertex_is_returned_for_broadcast()
    {
        await using var node = await TestNode.StartAsync();
        await node.SignIn("connection-2", TestKeys.PrivateKey2);

        var result = await node.Send(new UpdateLocalAdjacencyCommand([TestKeys.Address2], true));

        Assert.True(result.Success);
        Assert.Equal(TestKeys.PublicKey1, result.Value!.PublicKey);
        Assert.Equal([TestKeys.Address2], result.Value.Neighborhood.Neighbors);
        Assert.Equal([TestKeys.Address2], await node.Get<NetworkGraph>().GetNeighborAddressesAsync());
    }

    [Fact]
    public async Task An_address_without_a_session_is_not_added()
    {
        await using var node = await TestNode.StartAsync();
        await node.SignIn("connection-2", TestKeys.PrivateKey2);

        var result = await node.Send(new UpdateLocalAdjacencyCommand([TestKeys.Address2, TestKeys.Address3], true));

        Assert.True(result.Success);
        Assert.Equal([TestKeys.Address2], result.Value!.Neighborhood.Neighbors);
    }

    [Fact]
    public async Task Adding_signs_the_vertex_again_even_when_no_neighbor_is_new()
    {
        await using var node = await TestNode.StartAsync();
        var before = (await node.Get<NetworkGraph>().GetLocalVertexAsync()).Neighborhood.LastUpdate;
        await Task.Delay(20);

        var result = await node.Send(new UpdateLocalAdjacencyCommand([], true));

        Assert.True(result.Success);
        Assert.True(result.Value!.Neighborhood.LastUpdate > before);
    }

    [Fact]
    public async Task An_address_is_removed_whether_or_not_it_still_has_a_session()
    {
        await using var node = await TestNode.StartAsync();
        await node.SignIn("connection-2", TestKeys.PrivateKey2);
        await node.Send(new UpdateLocalAdjacencyCommand([TestKeys.Address2], true));

        var result = await node.Send(new UpdateLocalAdjacencyCommand([TestKeys.Address2], false));

        Assert.True(result.Success);
        Assert.Empty(result.Value!.Neighborhood.Neighbors!);
        Assert.Empty(await node.Get<NetworkGraph>().GetNeighborAddressesAsync());
    }

    [Fact]
    public async Task A_value_that_is_not_an_address_is_refused()
    {
        await using var node = await TestNode.StartAsync();

        Assert.False((await node.Send(new UpdateLocalAdjacencyCommand(["not-an-address"], true))).Success);
        Assert.False((await node.Send(new UpdateLocalAdjacencyCommand(["not-an-address"], false))).Success);
    }

    [Fact]
    public async Task On_a_locked_node_the_command_fails_because_no_vertex_can_be_signed()
    {
        await using var node = await TestNode.StartAsync();
        await node.SignIn("connection-2", TestKeys.PrivateKey2);
        node.Key.Locked = true;

        var result = await node.Send(new UpdateLocalAdjacencyCommand([TestKeys.Address2], true));

        Assert.False(result.Success);
        Assert.Empty(await node.Get<NetworkGraph>().GetNeighborAddressesAsync());
    }
}
