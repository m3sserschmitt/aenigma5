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

// The node has key 1. Key 2 belongs to another node.
public class BroadcastHandlerTests
{
    private static async Task<VertexBroadcastRequestDto> VertexOfKey2(params string[] neighbors)
    => (await Vertex.Factory.CreateAsync(FixedKeyCertificateManager.Key2(), [.. neighbors])).ToVertexBroadcast();

    [Fact]
    public async Task A_vertex_from_a_node_with_a_session_makes_that_node_a_neighbor()
    {
        await using var node = await TestNode.StartAsync();
        await node.SignIn("connection-2", TestKeys.PrivateKey2);

        var result = await node.Send(new HandleBroadcastCommand(await VertexOfKey2(TestKeys.Address1)));

        Assert.True(result.Success);
        // What must be passed on: the received vertex, and the node's own, which now lists the sender.
        Assert.Equal([TestKeys.Address2, TestKeys.Address1], result.Value!.Select(vertex => vertex.Neighborhood.Address).Order());
        Assert.Equal([TestKeys.Address2], await node.Get<NetworkGraph>().GetNeighborAddressesAsync());
    }

    [Fact]
    public async Task A_vertex_from_a_node_without_a_session_is_stored_but_gives_no_neighbor()
    {
        await using var node = await TestNode.StartAsync();

        var result = await node.Send(new HandleBroadcastCommand(await VertexOfKey2(TestKeys.Address1)));

        Assert.True(result.Success);
        Assert.Equal([TestKeys.Address2], result.Value!.Select(vertex => vertex.Neighborhood.Address));
        Assert.Empty(await node.Get<NetworkGraph>().GetNeighborAddressesAsync());
        Assert.NotNull(await node.Get<NetworkGraph>().GetVertexAsync(TestKeys.Address2));
    }

    [Fact]
    public async Task A_vertex_the_node_already_has_gives_nothing_to_pass_on()
    {
        await using var node = await TestNode.StartAsync();
        var vertex = await VertexOfKey2();
        await node.Send(new HandleBroadcastCommand(vertex));

        var result = await node.Send(new HandleBroadcastCommand(vertex));

        Assert.True(result.Success);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task A_request_without_usable_key_or_data_is_refused()
    {
        await using var node = await TestNode.StartAsync();
        var valid = await VertexOfKey2();

        Assert.False((await node.Send(new HandleBroadcastCommand(new VertexBroadcastRequestDto("not a key", valid.SignedData)))).Success);
        Assert.False((await node.Send(new HandleBroadcastCommand(new VertexBroadcastRequestDto(valid.PublicKey, "not base64!")))).Success);
    }

    [Fact]
    public async Task A_vertex_signed_with_another_key_is_dropped()
    {
        await using var node = await TestNode.StartAsync();
        var valid = await VertexOfKey2();

        var result = await node.Send(new HandleBroadcastCommand(new VertexBroadcastRequestDto(TestKeys.PublicKey3, valid.SignedData)));

        Assert.True(result.Success);
        Assert.Empty(result.Value!);
        Assert.Null(await node.Get<NetworkGraph>().GetVertexAsync(TestKeys.Address2));
    }
}
