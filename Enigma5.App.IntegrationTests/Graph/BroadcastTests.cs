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

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Enigma5.App.Models;
using Enigma5.Tests.Base;

namespace Enigma5.App.IntegrationTests.Graph;

// Node A has key 1. Keys 2 and 3 act as other nodes that connect to it.
public class BroadcastTests(NodeAFixture fixture) : IClassFixture<NodeAFixture>
{
    private readonly NodeProcess _node = fixture.Node;

    private async Task<List<string>> NeighborsOfTheNode()
    {
        var vertex = await _node.Public.GetFromJsonAsync<JsonElement>("/LocalVertex");
        return [.. vertex.GetProperty("neighborhood").GetProperty("neighbors").EnumerateArray().Select(item => item.GetString()!)];
    }

    [Fact]
    public async Task A_connected_node_that_lists_the_node_becomes_its_neighbor_and_stops_being_one_when_it_disconnects()
    {
        var other = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey2);

        var result = await other.Invoke<bool>("Broadcast", TestVertices.Signed(TestKeys.PrivateKey2, _node.Address));

        Assert.True(result.Success, result.Error);
        Assert.Contains(TestKeys.Address2, await NeighborsOfTheNode());
        Assert.Equal(HttpStatusCode.OK, (await _node.Public.GetAsync($"/Vertex?address={TestKeys.Address2}")).StatusCode);

        await other.DisposeAsync();

        Assert.True(await NodeProcess.Eventually(async () => !(await NeighborsOfTheNode()).Contains(TestKeys.Address2)));
    }

    [Fact]
    public async Task A_vertex_whose_owner_is_not_connected_is_stored_but_gives_no_neighbor()
    {
        await using var carrier = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey3);

        var result = await carrier.Invoke<bool>("Broadcast", TestVertices.Signed(TestKeys.PrivateKey2, _node.Address));

        Assert.True(result.Success, result.Error);
        Assert.Equal(HttpStatusCode.OK, (await _node.Public.GetAsync($"/Vertex?address={TestKeys.Address2}")).StatusCode);
        Assert.DoesNotContain(TestKeys.Address2, await NeighborsOfTheNode());
    }

    [Fact]
    public async Task A_vertex_that_does_not_match_its_signature_is_not_stored()
    {
        await using var other = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey3);
        var signedByKey3 = TestVertices.Signed(TestKeys.PrivateKey3);

        await other.Invoke<bool>("Broadcast", new VertexBroadcastRequestDto(TestKeys.PublicKey2, signedByKey3.SignedData));

        Assert.Equal(HttpStatusCode.NotFound, (await _node.Public.GetAsync($"/Vertex?address={TestKeys.Address3}")).StatusCode);
    }

    [Fact]
    public async Task A_broadcast_needs_a_session()
    {
        await using var client = await HubClient.ConnectAsync(_node.PublicUrl);

        var result = await client.Invoke<bool>("Broadcast", TestVertices.Signed(TestKeys.PrivateKey2));

        Assert.False(result.Success);
        Assert.Equal("Authentication required", result.Error);
    }
}
