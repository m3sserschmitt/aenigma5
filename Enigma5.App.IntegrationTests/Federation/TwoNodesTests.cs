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

using System.Net.Http.Json;
using System.Text.Json;
using Enigma5.Tests.Base;

namespace Enigma5.App.IntegrationTests.Federation;

// Node B (key 2) has node A (key 1) as its peer and connects to it. The client has key 3.
public class TwoNodesTests(TwoNodesFixture fixture) : IClassFixture<TwoNodesFixture>, IAsyncLifetime
{
    private readonly NodeProcess _a = fixture.A;

    private readonly NodeProcess _b = fixture.B;

    private static async Task<List<string>> Neighbors(NodeProcess node)
    {
        var vertex = await node.Public.GetFromJsonAsync<JsonElement>("/LocalVertex");
        return [.. vertex.GetProperty("neighborhood").GetProperty("neighbors").EnumerateArray().Select(item => item.GetString()!)];
    }

    private static async Task<List<string>> KnownAddresses(NodeProcess node)
    {
        var vertices = await node.Public.GetFromJsonAsync<JsonElement>("/Vertices");
        return [.. vertices.EnumerateArray().Select(item => item.GetProperty("neighborhood").GetProperty("address").GetString()!)];
    }

    // Every test starts when the two nodes list each other.
    public async Task InitializeAsync()
    {
        var connected = await NodeProcess.Eventually(async () => (await Neighbors(_a)).Contains(_b.Address) && (await Neighbors(_b)).Contains(_a.Address), seconds: 60);
        Assert.True(connected, $"The nodes did not connect. Log of B:{Environment.NewLine}{_b.Log}");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Each_node_holds_the_vertex_of_the_other()
    {
        Assert.True(await NodeProcess.Eventually(async () => (await KnownAddresses(_a)).Contains(_b.Address)));
        Assert.True(await NodeProcess.Eventually(async () => (await KnownAddresses(_b)).Contains(_a.Address)));
    }

    [Fact]
    public async Task A_message_goes_from_the_connecting_node_to_the_node_it_connected_to()
    {
        var before = _a.Pending(TestKeys.Address3);
        await using var sender = await HubClient.SignedInAsync(_b.PublicUrl, TestKeys.PrivateKey3);

        Assert.True((await sender.Route(null, TestOnions.ForClientThroughBThenA)).Success);

        Assert.True(await NodeProcess.Eventually(() => Task.FromResult(_a.Pending(TestKeys.Address3) == before + 1)));
    }

    [Fact]
    public async Task A_message_goes_from_the_node_that_was_connected_to_back_to_the_connecting_node()
    {
        var before = _b.Pending(TestKeys.Address3);
        await using var sender = await HubClient.SignedInAsync(_a.PublicUrl, TestKeys.PrivateKey3);

        Assert.True((await sender.Route(null, TestOnions.ForClientThroughAThenB)).Success);

        Assert.True(await NodeProcess.Eventually(() => Task.FromResult(_b.Pending(TestKeys.Address3) == before + 1)));
    }

    [Fact]
    public async Task A_client_that_is_connected_receives_the_relayed_message_at_once()
    {
        await using var recipient = await HubClient.SignedInAsync(_a.PublicUrl, TestKeys.PrivateKey3);
        await using var sender = await HubClient.SignedInAsync(_b.PublicUrl, TestKeys.PrivateKey3);

        Assert.True((await sender.Route(null, TestOnions.ForClientThroughBThenA)).Success);

        Assert.True(await NodeProcess.Eventually(() => Task.FromResult(!recipient.RoutedMessages.IsEmpty)));
    }

    [Fact]
    public async Task A_relayed_message_keeps_one_uuid_on_both_nodes()
    {
        var uuid = Guid.NewGuid().ToString();
        await using var sender = await HubClient.SignedInAsync(_b.PublicUrl, TestKeys.PrivateKey3);

        Assert.True((await sender.Route(uuid, TestOnions.ForClientThroughBThenA)).Success);

        Assert.True(await NodeProcess.Eventually(() => Task.FromResult(_a.Count("SELECT COUNT(*) FROM Messages WHERE Uuid = $u", ("$u", uuid)) == 1)));
    }
}
