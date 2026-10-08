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

public class CleanupMessagesHandlerTests
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(14);

    private static readonly TimeSpan DeliveredRetention = TimeSpan.FromHours(1);

    private static long SecondsAgo(TimeSpan age) => (DateTimeOffset.UtcNow - age).ToUnixTimeSeconds();

    private static PendingMessage Message(TimeSpan age, TimeSpan? deliveredAgo = null) => new()
    {
        Destination = TestKeys.Address2,
        Content = "dGVzdC1zdHJpbmc=",
        Timestamp = SecondsAgo(age),
        Sent = deliveredAgo is not null,
        SentTimestamp = deliveredAgo is null ? null : SecondsAgo(deliveredAgo.Value)
    };

    private static async Task<List<long>> Stored(TestNode node, params PendingMessage[] messages)
    {
        await node.Database(async context =>
        {
            context.Messages.AddRange(messages);
            await context.SaveChangesAsync();
        });
        return [.. messages.Select(message => message.Id)];
    }

    private static Task<List<long>> Left(TestNode node)
    => node.Database(context => context.Messages.OrderBy(message => message.Id).Select(message => message.Id).ToListAsync());

    [Fact]
    public async Task Pending_messages_older_than_the_retention_period_are_removed()
    {
        await using var node = await TestNode.StartAsync();
        var ids = await Stored(node, Message(TimeSpan.FromDays(15)), Message(TimeSpan.FromDays(13)));

        var result = await node.Send(new CleanupMessagesCommand(Retention, DeliveredRetention));

        Assert.True(result.Success);
        Assert.Equal(1, result.Value);
        Assert.Equal([ids[1]], await Left(node));
    }

    [Fact]
    public async Task Delivered_messages_are_removed_once_their_own_period_has_passed_since_delivery()
    {
        await using var node = await TestNode.StartAsync();
        var ids = await Stored(node,
            Message(TimeSpan.FromDays(2), deliveredAgo: TimeSpan.FromHours(2)),
            Message(TimeSpan.FromDays(2), deliveredAgo: TimeSpan.FromMinutes(10)));

        var result = await node.Send(new CleanupMessagesCommand(Retention, DeliveredRetention));

        Assert.Equal(1, result.Value);
        Assert.Equal([ids[1]], await Left(node));
    }

    [Fact]
    public async Task With_a_period_of_zero_delivered_messages_are_removed_at_the_next_run()
    {
        await using var node = await TestNode.StartAsync();
        var ids = await Stored(node, Message(TimeSpan.FromMinutes(5), deliveredAgo: TimeSpan.FromSeconds(5)), Message(TimeSpan.FromMinutes(5)));

        var result = await node.Send(new CleanupMessagesCommand(Retention, TimeSpan.Zero));

        Assert.Equal(1, result.Value);
        Assert.Equal([ids[1]], await Left(node));
    }

    // A delivered message is judged by its delivery time only, however old it is.
    [Fact]
    public async Task An_old_message_that_was_delivered_a_moment_ago_is_kept_for_its_own_period()
    {
        await using var node = await TestNode.StartAsync();
        var ids = await Stored(node, Message(TimeSpan.FromDays(30), deliveredAgo: TimeSpan.FromMinutes(1)));

        var result = await node.Send(new CleanupMessagesCommand(Retention, DeliveredRetention));

        Assert.Equal(0, result.Value);
        Assert.Equal(ids, await Left(node));
    }
}
