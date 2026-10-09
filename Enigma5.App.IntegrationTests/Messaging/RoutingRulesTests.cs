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

// Node A has key 1. The sender has key 2 and the recipient key 3; neither is a neighbor of the node at first.
public class RoutingRulesTests(NodeAFixture fixture) : IClassFixture<NodeAFixture>
{
    private readonly NodeProcess _node = fixture.Node;

    private Task<HubClient> SenderAsync() => HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey2);

    private long StoredWithUuid(string uuid) => _node.Count("SELECT COUNT(*) FROM Messages WHERE Uuid = $u", ("$u", uuid));

    [Fact]
    public async Task A_client_may_send_a_single_message_without_uuid()
    {
        await using var sender = await SenderAsync();

        Assert.True((await sender.Route(null, TestOnions.ForClientThroughA1)).Success);
    }

    [Fact]
    public async Task A_client_may_send_several_messages_in_one_request_without_uuid()
    {
        await using var sender = await SenderAsync();
        var before = _node.Pending(TestKeys.Address3);

        var result = await sender.Route(null, TestOnions.ForClientThroughA1, TestOnions.ForClientThroughA2, TestOnions.ForClientThroughA3);

        Assert.True(result.Success);
        Assert.Equal(before + 3, _node.Pending(TestKeys.Address3));
    }

    [Fact]
    public async Task A_uuid_is_stored_in_lower_case_whatever_its_form_in_the_request()
    {
        await using var sender = await SenderAsync();
        var uuid = Guid.NewGuid().ToString();

        Assert.True((await sender.Route(uuid.ToUpperInvariant(), TestOnions.ForClientThroughA1)).Success);

        Assert.Equal(1, StoredWithUuid(uuid));
    }

    [Fact]
    public async Task A_message_whose_uuid_is_already_stored_is_accepted_but_not_stored_again()
    {
        await using var sender = await SenderAsync();
        var uuid = Guid.NewGuid().ToString();

        var first = await sender.Route(uuid, TestOnions.ForClientThroughA1);
        var second = await sender.Route(uuid, TestOnions.ForClientThroughA1);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(1, StoredWithUuid(uuid));
    }

    // One connection is served one call at a time and one key has one session, so the parallel callers use two keys.
    [Fact]
    public async Task Many_requests_at_once_with_one_uuid_store_one_message()
    {
        var uuid = Guid.NewGuid().ToString();
        await using var first = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey2);
        await using var second = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey3);
        HubClient[] senders = [first, second];

        var results = await Task.WhenAll(senders.SelectMany(sender => Enumerable.Range(0, 4).Select(_ => sender.Route(uuid, TestOnions.ForClientThroughA2))));

        Assert.All(results, result => Assert.True(result.Success, result.Error));
        Assert.Equal(1, StoredWithUuid(uuid));
    }

    [Fact]
    public async Task A_uuid_with_several_messages_is_refused()
    {
        await using var sender = await SenderAsync();
        var uuid = Guid.NewGuid().ToString();

        var result = await sender.Route(uuid, TestOnions.ForClientThroughA1, TestOnions.ForClientThroughA2);

        Assert.False(result.Success);
        Assert.Equal("A uuid can only be provided with a single payload.", result.Error);
        Assert.Equal(0, StoredWithUuid(uuid));
    }

    [Fact]
    public async Task A_uuid_that_is_not_a_guid_is_refused()
    {
        await using var sender = await SenderAsync();

        var result = await sender.Route("abc", TestOnions.ForClientThroughA1);

        Assert.False(result.Success);
        Assert.Equal("One or more properties not in correct format.", result.Error);
    }

    [Fact]
    public async Task An_onion_that_was_sealed_for_another_node_is_refused()
    {
        await using var sender = await SenderAsync();

        var result = await sender.Route(null, TestOnions.ForClientThroughB);

        Assert.False(result.Success);
        Assert.Equal("Could not parse onion.", result.Error);
    }

    [Theory]
    [InlineData("AA==")]
    [InlineData("//8=")]
    [InlineData("AAEC")]
    public async Task Text_that_is_not_an_onion_is_refused_and_the_node_keeps_working(string onion)
    {
        await using var sender = await SenderAsync();

        var refused = await sender.Route(null, onion);
        var afterwards = await sender.Route(null, TestOnions.ForClientThroughA1);

        Assert.False(refused.Success);
        Assert.Equal("Could not parse onion.", refused.Error);
        Assert.True(afterwards.Success);
    }

    [Fact]
    public async Task In_a_request_with_good_and_bad_onions_the_good_ones_are_stored()
    {
        await using var sender = await SenderAsync();
        var before = _node.Pending(TestKeys.Address3);

        var result = await sender.Route(null, TestOnions.ForClientThroughA1, TestOnions.ForClientThroughB, TestOnions.ForClientThroughA2);

        Assert.False(result.Success);
        Assert.Equal(before + 2, _node.Pending(TestKeys.Address3));
    }

    [Fact]
    public async Task Routing_needs_a_session()
    {
        await using var client = await HubClient.ConnectAsync(_node.PublicUrl);

        var result = await client.Route(null, TestOnions.ForClientThroughA1);

        Assert.False(result.Success);
        Assert.Equal("Authentication required", result.Error);
    }
}
