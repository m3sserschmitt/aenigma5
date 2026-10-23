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

using Enigma5.App.Hubs;
using Enigma5.App.Models;
using Enigma5.Tests.Base;
using static Enigma5.App.Tests.Hubs.HubTesting;

namespace Enigma5.App.Tests.Hubs;

// The hub methods of the second generation (RoutingHub2.cs), called directly. The node has key 1.
public class RoutingHub2Tests
{
    [Fact]
    public async Task Pull2_returns_the_messages_of_the_caller_only()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address3, 3);
        await Store(node, TestKeys.Address2, 2);
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.ClientAddress = TestKeys.Address3;

        var page = await connection.Hub.Pull2(new PullRequestDto());

        Assert.Equal(3, page.Data!.Count);
        Assert.All(page.Data, message => Assert.Equal(TestKeys.Address3, message.Destination));
    }

    [Fact]
    public async Task Pull2_returns_at_most_one_page_of_messages_starting_with_the_oldest()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address3, Common.Constants.MessagesPageSize + 5);
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.ClientAddress = TestKeys.Address3;

        var page = await connection.Hub.Pull2(new PullRequestDto());

        Assert.Equal(Common.Constants.MessagesPageSize, page.Data!.Count);
        Assert.Equal(page.Data.Select(message => message.Id), page.Data.Select(message => message.Id).Order());
    }

    [Fact]
    public async Task Pull2_continues_after_the_given_message()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address3, Common.Constants.MessagesPageSize + 5);
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.ClientAddress = TestKeys.Address3;
        var first = await connection.Hub.Pull2(new PullRequestDto());

        var afterTheFirstMessage = await connection.Hub.Pull2(new PullRequestDto(first.Data![0].Id));
        var afterTheFirstPage = await connection.Hub.Pull2(new PullRequestDto(first.Data[^1].Id));

        Assert.Equal(first.Data.Skip(1).Select(message => message.Id), afterTheFirstMessage.Data!.Select(message => message.Id).Take(first.Data.Count - 1));
        Assert.Equal(5, afterTheFirstPage.Data!.Count);
        Assert.All(afterTheFirstPage.Data, message => Assert.True(message.Id > first.Data[^1].Id));
    }

    [Fact]
    public async Task Pull2_and_Cleanup2_fail_when_the_caller_has_no_address()
    {
        await using var node = await TestNode.StartAsync();
        await using var connection = new HubConnection(node, Connection);

        Assert.Equal(InvocationErrors.INTERNAL_ERROR, SingleError(await connection.Hub.Pull2(new PullRequestDto())));
        Assert.Equal(InvocationErrors.INTERNAL_ERROR, SingleError(await connection.Hub.Cleanup2(new CleanupRequestDto())));
    }

    [Fact]
    public async Task Cleanup2_confirms_nothing_on_a_connection_that_has_pulled_nothing()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address3, 3);
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.ClientAddress = TestKeys.Address3;

        var result = await connection.Hub.Cleanup2(new CleanupRequestDto(long.MaxValue));

        Assert.True(result.Success);
        Assert.Equal(0, await Confirmed(node));
    }

    [Fact]
    public async Task Cleanup2_confirms_up_to_the_given_message()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address3, 3);
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.ClientAddress = TestKeys.Address3;
        var pulled = await connection.Hub.Pull2(new PullRequestDto());

        var result = await connection.Hub.Cleanup2(new CleanupRequestDto(pulled.Data![1].Id));

        Assert.True(result.Success);
        Assert.Equal(2, await Confirmed(node));
    }

    [Fact]
    public async Task Cleanup2_never_confirms_beyond_what_the_connection_has_pulled()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address3, 2);
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.ClientAddress = TestKeys.Address3;
        await connection.Hub.Pull2(new PullRequestDto());
        await Store(node, TestKeys.Address3, 2);

        var result = await connection.Hub.Cleanup2(new CleanupRequestDto(long.MaxValue));

        Assert.True(result.Success);
        Assert.Equal(2, await Confirmed(node));
    }
}
