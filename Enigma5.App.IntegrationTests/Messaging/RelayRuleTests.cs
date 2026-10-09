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

using Enigma5.Tests.Base;

namespace Enigma5.App.IntegrationTests.Messaging;

// A caller that the node lists as neighbor is another relay. It must pass on one message at a time, with its uuid.
public class RelayRuleTests(NodeAFixture fixture) : IClassFixture<NodeAFixture>
{
    private readonly NodeProcess _node = fixture.Node;

    private async Task<HubClient> NeighborAsync()
    {
        var relay = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey2);
        Assert.True((await relay.Invoke<bool>("Broadcast", TestVertices.Signed(TestKeys.PrivateKey2, _node.Address))).Success);
        return relay;
    }

    [Fact]
    public async Task A_relay_may_send_one_message_with_its_uuid()
    {
        await using var relay = await NeighborAsync();

        Assert.True((await relay.Route(Guid.NewGuid().ToString(), TestOnions.ForClientThroughA1)).Success);
    }

    [Fact]
    public async Task A_relay_may_not_send_a_message_without_uuid()
    {
        await using var relay = await NeighborAsync();

        var result = await relay.Route(null, TestOnions.ForClientThroughA1);

        Assert.False(result.Success);
        Assert.Equal("Relays must route one message per request, with its uuid.", result.Error);
    }

    [Fact]
    public async Task A_relay_may_not_send_several_messages_in_one_request()
    {
        await using var relay = await NeighborAsync();
        var before = _node.Pending(TestKeys.Address3);

        var result = await relay.Route(null, TestOnions.ForClientThroughA1, TestOnions.ForClientThroughA2);

        Assert.False(result.Success);
        Assert.Equal("Relays must route one message per request, with its uuid.", result.Error);
        Assert.Equal(before, _node.Pending(TestKeys.Address3));
    }

    [Fact]
    public async Task The_same_key_is_free_of_the_rule_while_it_is_not_a_neighbor()
    {
        await using var client = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey3);

        Assert.True((await client.Route(null, TestOnions.ForClientThroughA1, TestOnions.ForClientThroughA2)).Success);
    }
}
