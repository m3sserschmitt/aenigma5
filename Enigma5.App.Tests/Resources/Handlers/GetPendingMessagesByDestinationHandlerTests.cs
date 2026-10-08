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
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Queries;
using Enigma5.Tests.Base;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Tests.Resources.Handlers;

public class GetPendingMessagesByDestinationHandlerTests
{
    private const string Content = "dGVzdC1zdHJpbmc=";

    private static async Task<List<long>> Store(TestNode node, string destination, int count)
    {
        var ids = new List<long>();
        for (var index = 0; index < count; index++)
        {
            ids.Add((await node.Send(new CreatePendingMessageCommand(destination, Content, null))).Value!.Id);
        }
        return ids;
    }

    [Fact]
    public async Task The_first_page_holds_the_oldest_messages_of_the_destination_in_order()
    {
        await using var node = await TestNode.StartAsync();
        var ids = await Store(node, TestKeys.Address2, 5);
        await Store(node, TestKeys.Address3, 2);

        var page = await node.Send(new GetPendingMessagesByDestinationQuery(TestKeys.Address2, null, 3));

        Assert.True(page.Success);
        Assert.Equal(ids.Take(3), page.Value!.Select(message => message.Id));
        Assert.All(page.Value!, message => Assert.Equal(TestKeys.Address2, message.Destination));
    }

    [Fact]
    public async Task The_next_page_starts_after_the_given_id()
    {
        await using var node = await TestNode.StartAsync();
        var ids = await Store(node, TestKeys.Address2, 5);

        var page = await node.Send(new GetPendingMessagesByDestinationQuery(TestKeys.Address2, ids[2], 20));

        Assert.Equal(ids.Skip(3), page.Value!.Select(message => message.Id));
    }

    [Fact]
    public async Task Delivered_messages_are_left_out()
    {
        await using var node = await TestNode.StartAsync();
        var ids = await Store(node, TestKeys.Address2, 3);
        await node.Send(new MarkMessagesAsDeliveredCommand(TestKeys.Address2, ids[0]));

        var page = await node.Send(new GetPendingMessagesByDestinationQuery(TestKeys.Address2, null, 20));

        Assert.Equal(ids.Skip(1), page.Value!.Select(message => message.Id));
    }

    [Fact]
    public async Task A_destination_without_messages_gets_an_empty_page()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address2, 1);

        var page = await node.Send(new GetPendingMessagesByDestinationQuery(TestKeys.Address3, null, 20));

        Assert.True(page.Success);
        Assert.Empty(page.Value!);
    }
}
