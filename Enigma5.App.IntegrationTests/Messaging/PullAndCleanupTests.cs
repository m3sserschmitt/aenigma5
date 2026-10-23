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

using Enigma5.App.Models;
using Enigma5.Tests.Base;

namespace Enigma5.App.IntegrationTests.Messaging;

// Node A stores messages for the client with key 3. A second client, with key 2, sends them.
public class PullAndCleanupTests(NodeAFixture fixture) : IClassFixture<NodeAFixture>, IAsyncLifetime
{
    private readonly NodeProcess _node = fixture.Node;

    private static readonly string Recipient = TestKeys.Address3;

    // Every test starts with an empty mailbox.
    public async Task InitializeAsync()
    {
        await using var recipient = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey3);
        while (true)
        {
            var page = await recipient.Pull2();
            if (page.Data!.Count == 0)
            {
                break;
            }
            await recipient.Invoke<bool>("Cleanup2", new CleanupRequestDto(page.Data[^1].Id));
        }
        Assert.Equal(0, _node.Pending(Recipient));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task Store(int count)
    {
        await using var sender = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey2);
        for (var stored = 0; stored < count; stored += 20)
        {
            var batch = Enumerable.Repeat<string?>(TestOnions.ForClientThroughA1, Math.Min(20, count - stored)).ToArray();
            Assert.True((await sender.Route(null, batch)).Success);
        }
        Assert.Equal(count, _node.Pending(Recipient));
    }

    private Task<HubClient> RecipientAsync() => HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey3);

    [Fact]
    public async Task Stored_messages_are_pulled_in_pages_of_twenty_in_the_order_they_arrived()
    {
        await Store(45);
        await using var recipient = await RecipientAsync();

        var first = (await recipient.Pull2()).Data!;
        var second = (await recipient.Pull2(first[^1].Id)).Data!;
        var third = (await recipient.Pull2(second[^1].Id)).Data!;
        var fourth = (await recipient.Pull2(third[^1].Id)).Data!;

        Assert.Equal([20, 20, 5, 0], [first.Count, second.Count, third.Count, fourth.Count]);
        var ids = first.Concat(second).Concat(third).Select(message => message.Id).ToList();
        Assert.Equal(ids.Order(), ids);
        Assert.Equal(45, ids.Distinct().Count());
        Assert.All(first, message => Assert.Equal(Recipient, message.Destination));
    }

    [Fact]
    public async Task Pulling_alone_confirms_nothing()
    {
        await Store(5);
        await using var recipient = await RecipientAsync();

        await recipient.Pull2();
        await recipient.Pull2();

        Assert.Equal(5, _node.Pending(Recipient));
    }

    [Fact]
    public async Task Cleanup2_confirms_the_page_that_was_pulled_and_nothing_after_it()
    {
        await Store(45);
        await using var recipient = await RecipientAsync();
        var page = (await recipient.Pull2()).Data!;

        var result = await recipient.Invoke<bool>("Cleanup2", new CleanupRequestDto(page[^1].Id));

        Assert.True(result.Success);
        Assert.Equal(25, _node.Pending(Recipient));
        Assert.Equal(page[^1].Id + 1, (await recipient.Pull2()).Data![0].Id);
    }

    [Fact]
    public async Task Cleanup2_with_an_id_inside_the_page_confirms_up_to_that_id()
    {
        await Store(20);
        await using var recipient = await RecipientAsync();
        var page = (await recipient.Pull2()).Data!;

        await recipient.Invoke<bool>("Cleanup2", new CleanupRequestDto(page[4].Id));

        Assert.Equal(15, _node.Pending(Recipient));
    }

    [Fact]
    public async Task Cleanup2_never_confirms_beyond_what_the_connection_has_pulled()
    {
        await Store(45);
        await using var recipient = await RecipientAsync();
        await recipient.Pull2();

        await recipient.Invoke<bool>("Cleanup2", new CleanupRequestDto(long.MaxValue));

        Assert.Equal(25, _node.Pending(Recipient));
    }

    [Fact]
    public async Task Messages_that_arrive_after_the_pull_are_not_confirmed_by_the_cleanup_that_follows()
    {
        await Store(5);
        await using var recipient = await RecipientAsync();
        await recipient.Pull2();
        await using var sender = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey2);
        await sender.Route(null, TestOnions.ForClientThroughA2, TestOnions.ForClientThroughA3);

        await recipient.Invoke<bool>("Cleanup2", new CleanupRequestDto(long.MaxValue));

        Assert.Equal(2, _node.Pending(Recipient));
    }

    [Fact]
    public async Task A_cleanup_on_a_connection_that_has_pulled_nothing_confirms_nothing()
    {
        await Store(5);
        await using var recipient = await RecipientAsync();

        Assert.True((await recipient.Invoke<bool>("Cleanup2", new CleanupRequestDto(long.MaxValue))).Success);
        Assert.True((await recipient.Invoke<bool>("Cleanup")).Success);

        Assert.Equal(5, _node.Pending(Recipient));
    }

    // The older methods: Pull returns at most one page of 20 messages, and Cleanup must not confirm the rest.
    [Fact]
    public async Task The_older_Pull_and_Cleanup_do_not_lose_messages_beyond_the_first_page()
    {
        await Store(50);
        await using var recipient = await RecipientAsync();

        var pulled = await recipient.Invoke<List<PendingMessageDto>>("Pull");
        var cleaned = await recipient.Invoke<bool>("Cleanup");

        Assert.Equal(20, pulled.Data!.Count);
        Assert.True(cleaned.Success);
        Assert.Equal(30, _node.Pending(Recipient));
        Assert.Equal(20, (await recipient.Invoke<List<PendingMessageDto>>("Pull")).Data!.Count);
    }

    // A copy that arrives after the recipient confirmed the message, as a relay may send it, is recognized by its uuid.
    [Fact]
    public async Task A_message_that_arrives_again_after_it_was_confirmed_is_not_delivered_again()
    {
        var uuid = Guid.NewGuid().ToString();
        await using var sender = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey2);
        Assert.True((await sender.Route(uuid, TestOnions.ForClientThroughA1)).Success);
        await using (var recipient = await RecipientAsync())
        {
            var page = await recipient.Pull2();
            Assert.Equal(uuid, Assert.Single(page.Data!).Uuid);
            Assert.True((await recipient.Invoke<bool>("Cleanup2", new CleanupRequestDto(page.Data![0].Id))).Success);
        }

        var again = await sender.Route(uuid, TestOnions.ForClientThroughA1);

        Assert.True(again.Success);
        Assert.Equal(0, _node.Pending(Recipient));
        Assert.Equal(1, _node.Count("SELECT COUNT(*) FROM Messages WHERE Uuid = $u", ("$u", uuid)));
        Assert.Equal(1, _node.Count("SELECT COUNT(*) FROM Messages WHERE Uuid = $u AND Sent = 1 AND Content IS NULL", ("$u", uuid)));
        await using var later = await RecipientAsync();
        Assert.Empty((await later.Pull2()).Data!);
    }

    [Fact]
    public async Task A_connected_recipient_gets_the_message_at_once_and_it_stays_stored_until_confirmed()
    {
        await using var recipient = await RecipientAsync();
        await using var sender = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey2);

        await sender.Route(null, TestOnions.ForClientThroughA1);

        Assert.True(await NodeProcess.Eventually(() => Task.FromResult(!recipient.RoutedMessages.IsEmpty)));
        Assert.True(recipient.RoutedMessages.TryPeek(out var pushed));
        Assert.Equal("message 1", System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(pushed.GetProperty("payloads")[0].GetString()!)));
        Assert.True(Guid.TryParse(pushed.GetProperty("uuid").GetString(), out _));
        Assert.Equal(1, _node.Pending(Recipient));
    }

    // The onion for node B, which is not connected, comes after one for the connected recipient in the same request.
    // The sender must not use key 2, the key of node B, which would then be connected. It uses key 1, the node's own
    // key, without asking to act for another address: it is then a plain client with the node's address.
    [Fact]
    public async Task In_a_request_with_several_messages_the_recipient_gets_only_its_own()
    {
        await using var recipient = await RecipientAsync();
        await using var sender = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey1);
        var forB = _node.Pending(TestKeys.Address2);

        Assert.True((await sender.Route(null, TestOnions.ForClientThroughA1, TestOnions.ForClientThroughAThenB)).Success);
        // A later message marks the end: once it has arrived, anything pushed for the first request has arrived too.
        Assert.True((await sender.Route(null, TestOnions.ForClientThroughA2)).Success);

        List<string> Pushed() => [.. recipient.RoutedMessages
            .SelectMany(message => message.GetProperty("payloads").EnumerateArray())
            .Select(payload => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload.GetString()!)))];
        Assert.True(await NodeProcess.Eventually(() => Task.FromResult(Pushed().Contains("message 2"))));
        Assert.Equal(["message 1", "message 2"], Pushed().Order());
        Assert.Equal(forB + 1, _node.Pending(TestKeys.Address2));
    }

    [Theory]
    [InlineData(null, "One or more required properties not provided.")]
    [InlineData(-1L, "One or more properties have invalid values.")]
    public async Task Cleanup2_refuses_a_missing_or_negative_id(long? supId, string expectedError)
    {
        await using var recipient = await RecipientAsync();

        var result = await recipient.Invoke<bool>("Cleanup2", new CleanupRequestDto(supId));

        Assert.False(result.Success);
        Assert.Equal(expectedError, result.Error);
    }

    [Fact]
    public async Task Pull2_refuses_a_negative_id()
    {
        await using var recipient = await RecipientAsync();

        var result = await recipient.Pull2(-1);

        Assert.False(result.Success);
        Assert.Equal("One or more properties have invalid values.", result.Error);
    }
}
