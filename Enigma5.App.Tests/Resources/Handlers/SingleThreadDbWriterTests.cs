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
using Enigma5.App.Resources.Contracts;
using Enigma5.Tests.Base;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Tests.Resources.Handlers;

public class SingleThreadDbWriterTests
{
    private const string Content = "dGVzdC1zdHJpbmc=";

    private static PendingMessage Message(string? uuid = null)
    {
        var message = new PendingMessage { Destination = TestKeys.Address2, Content = Content };
        if (uuid is not null)
        {
            message.Uuid = uuid;
        }
        return message;
    }

    #region Messages

    [Fact]
    public async Task A_message_is_stored_and_returned_with_its_id()
    {
        await using var node = await TestNode.StartAsync();
        var writer = node.Get<IDbWriter>();
        var message = Message();

        var stored = await writer.CreatePendingMessageAsync(message, skipIfUuidExists: false);

        Assert.Same(message, stored);
        Assert.True(stored!.Id > 0);
        Assert.Equal(1, await node.Database(context => context.Messages.CountAsync()));
    }

    [Fact]
    public async Task With_the_uuid_check_a_second_message_with_the_same_uuid_is_not_stored_and_the_first_is_returned()
    {
        await using var node = await TestNode.StartAsync();
        var writer = node.Get<IDbWriter>();
        var uuid = Guid.NewGuid().ToString();
        var first = await writer.CreatePendingMessageAsync(Message(uuid), skipIfUuidExists: true);
        var second = Message(uuid);

        var stored = await writer.CreatePendingMessageAsync(second, skipIfUuidExists: true);

        Assert.NotSame(second, stored);
        Assert.Equal(first!.Id, stored!.Id);
        Assert.Equal(1, await node.Database(context => context.Messages.CountAsync()));
    }

    [Fact]
    public async Task Many_calls_at_once_with_one_uuid_store_exactly_one_message()
    {
        await using var node = await TestNode.StartAsync();
        var writer = node.Get<IDbWriter>();
        var uuid = Guid.NewGuid().ToString();

        var stored = await Task.WhenAll(Enumerable.Range(0, 40).Select(_ => Task.Run(() => writer.CreatePendingMessageAsync(Message(uuid), skipIfUuidExists: true))));

        Assert.Single(stored.Select(message => message!.Id).Distinct());
        Assert.Equal(1, await node.Database(context => context.Messages.CountAsync()));
    }

    [Fact]
    public async Task Messages_are_marked_as_delivered_with_the_time_of_delivery()
    {
        await using var node = await TestNode.StartAsync();
        var writer = node.Get<IDbWriter>();
        var first = await writer.CreatePendingMessageAsync(Message(), false);
        var second = await writer.CreatePendingMessageAsync(Message(), false);
        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var changed = await writer.MarkMessagesAsDeliveredAsync(message => message.Id == first!.Id);

        Assert.Equal(1, changed);
        var messages = await node.Database(context => context.Messages.OrderBy(message => message.Id).ToListAsync());
        Assert.True(messages[0].Sent);
        Assert.NotNull(messages[0].DateSent);
        Assert.InRange(messages[0].SentTimestamp!.Value, before, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        Assert.False(messages[1].Sent);
        Assert.Null(messages[1].SentTimestamp);
        Assert.Equal(second!.Id, messages[1].Id);
    }

    [Fact]
    public async Task Messages_that_match_are_removed()
    {
        await using var node = await TestNode.StartAsync();
        var writer = node.Get<IDbWriter>();
        var first = await writer.CreatePendingMessageAsync(Message(), false);
        await writer.CreatePendingMessageAsync(Message(), false);

        Assert.Equal(1, await writer.RemoveMessagesAsync(message => message.Id == first!.Id));
        Assert.Equal(0, await writer.RemoveMessagesAsync(message => message.Id == first!.Id));

        Assert.Equal(1, await node.Database(context => context.Messages.CountAsync()));
    }

    #endregion

    #region Access counts

    [Fact]
    public async Task The_access_count_of_shared_data_goes_up_by_one_and_the_record_is_removed_at_its_maximum()
    {
        await using var node = await TestNode.StartAsync();
        var writer = node.Get<IDbWriter>();
        var data = new SharedData { Data = Content, PublicKey = TestKeys.PublicKey1, MaxAccessCount = 2 };
        Assert.Equal(1, await writer.CreateSharedDataAsync(data));

        var first = await writer.IncrementSharedDataAccessCountAsync(data.Tag);
        var stillStored = await node.Database(context => context.SharedData.CountAsync());
        var second = await writer.IncrementSharedDataAccessCountAsync(data.Tag);
        var third = await writer.IncrementSharedDataAccessCountAsync(data.Tag);

        Assert.Equal(1, first!.AccessCount);
        Assert.Equal(1, stillStored);
        Assert.Equal(2, second!.AccessCount);
        Assert.Null(third);
        Assert.Equal(0, await node.Database(context => context.SharedData.CountAsync()));
    }

    [Fact]
    public async Task Many_increments_at_once_are_all_counted()
    {
        await using var node = await TestNode.StartAsync();
        var writer = node.Get<IDbWriter>();
        var data = new SharedData { Data = Content, PublicKey = TestKeys.PublicKey1, MaxAccessCount = 1000 };
        var file = new FileRecord { MaxAccessCount = 1000 };
        await writer.CreateSharedDataAsync(data);
        await writer.CreateFileAsync(file);

        await Task.WhenAll(Enumerable.Range(0, 100).Select(index => Task.Run(async () =>
        {
            await writer.IncrementSharedDataAccessCountAsync(data.Tag);
            await writer.IncrementFileAccessCountAsync(file.Tag);
        })));

        Assert.Equal(100, await node.Database(context => context.SharedData.Select(item => item.AccessCount).SingleAsync()));
        Assert.Equal(100, await node.Database(context => context.Files.Select(item => item.AccessCount).SingleAsync()));
    }

    [Fact]
    public async Task With_many_increments_at_once_exactly_the_allowed_number_succeeds()
    {
        await using var node = await TestNode.StartAsync();
        var writer = node.Get<IDbWriter>();
        var file = new FileRecord { MaxAccessCount = 5 };
        await writer.CreateFileAsync(file);

        var results = await Task.WhenAll(Enumerable.Range(0, 40).Select(_ => Task.Run(() => writer.IncrementFileAccessCountAsync(file.Tag))));

        Assert.Equal(5, results.Count(record => record is not null));
        Assert.Equal([1, 2, 3, 4, 5], results.Where(record => record is not null).Select(record => record!.AccessCount).Order());
        Assert.Equal(0, await node.Database(context => context.Files.CountAsync()));
    }

    [Fact]
    public async Task An_unknown_tag_gives_no_record()
    {
        await using var node = await TestNode.StartAsync();
        var writer = node.Get<IDbWriter>();

        Assert.Null(await writer.IncrementSharedDataAccessCountAsync(Guid.NewGuid().ToString()));
        Assert.Null(await writer.IncrementFileAccessCountAsync(Guid.NewGuid().ToString()));
    }

    #endregion

    #region Peers, files and shared data

    [Fact]
    public async Task Peers_files_and_shared_data_are_created_and_removed()
    {
        await using var node = await TestNode.StartAsync();
        var writer = node.Get<IDbWriter>();
        var peer = new Peer { Host = "http://peer.example/", Address = TestKeys.Address2 };
        var file = new FileRecord();
        var data = new SharedData { Data = Content, PublicKey = TestKeys.PublicKey1 };

        Assert.Equal(1, await writer.CreatePeerAsync(peer));
        Assert.Equal(1, await writer.CreateFileAsync(file));
        Assert.Equal(1, await writer.CreateSharedDataAsync(data));
        Assert.True(peer.Id > 0);

        Assert.Equal(1, await writer.RemovePeerAsync(peer));
        Assert.Equal(1, await writer.RemoveFileAsync(file));
        Assert.Equal(1, await writer.RemoveSharedDataAsync(item => item.Tag == data.Tag));
        Assert.Equal(0, await node.Database(async context => await context.Peers.CountAsync() + await context.Files.CountAsync() + await context.SharedData.CountAsync()));
    }

    [Fact]
    public async Task A_cancelled_call_does_not_reach_the_database()
    {
        await using var node = await TestNode.StartAsync();
        var writer = node.Get<IDbWriter>();

        await Assert.ThrowsAsync<OperationCanceledException>(() => writer.CreatePendingMessageAsync(Message(), false, new CancellationToken(canceled: true)));

        Assert.Equal(0, await node.Database(context => context.Messages.CountAsync()));
    }

    #endregion
}
