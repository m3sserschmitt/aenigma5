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

public class MarkMessagesAsDeliveredHandlerTests
{
    private const string Content = "dGVzdC1zdHJpbmc=";

    private static async Task<long> Store(TestNode node, string destination)
    => (await node.Send(new CreatePendingMessageCommand(destination, Content, null))).Value!.Id;

    private static Task<List<long>> Pending(TestNode node)
    => node.Database(context => context.Messages.Where(message => !message.Sent).OrderBy(message => message.Id).Select(message => message.Id).ToListAsync());

    [Fact]
    public async Task Messages_of_the_destination_up_to_the_given_id_are_marked()
    {
        await using var node = await TestNode.StartAsync();
        var first = await Store(node, TestKeys.Address2);
        var second = await Store(node, TestKeys.Address2);
        var third = await Store(node, TestKeys.Address2);

        var result = await node.Send(new MarkMessagesAsDeliveredCommand(TestKeys.Address2, second));

        Assert.True(result.Success);
        Assert.Equal(2, result.Value);
        Assert.Equal([third], await Pending(node));
        Assert.NotEqual(first, third);
    }

    [Fact]
    public async Task Messages_of_other_destinations_are_left_alone()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address2);
        var other = await Store(node, TestKeys.Address3);

        var result = await node.Send(new MarkMessagesAsDeliveredCommand(TestKeys.Address2, long.MaxValue));

        Assert.Equal(1, result.Value);
        Assert.Equal([other], await Pending(node));
    }

    [Fact]
    public async Task Without_an_id_all_messages_of_the_destination_are_marked()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address2);
        await Store(node, TestKeys.Address2);

        var result = await node.Send(new MarkMessagesAsDeliveredCommand(TestKeys.Address2, null));

        Assert.Equal(2, result.Value);
        Assert.Empty(await Pending(node));
    }

    [Fact]
    public async Task A_message_is_not_marked_twice()
    {
        await using var node = await TestNode.StartAsync();
        var id = await Store(node, TestKeys.Address2);
        await node.Send(new MarkMessagesAsDeliveredCommand(TestKeys.Address2, id));

        var result = await node.Send(new MarkMessagesAsDeliveredCommand(TestKeys.Address2, id));

        Assert.True(result.Success);
        Assert.Equal(0, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-an-address")]
    public async Task A_destination_that_is_not_an_address_is_refused(string? destination)
    {
        await using var node = await TestNode.StartAsync();
        var id = await Store(node, TestKeys.Address2);

        var result = await node.Send(new MarkMessagesAsDeliveredCommand(destination, null));

        Assert.False(result.Success);
        Assert.Equal([id], await Pending(node));
    }
}
