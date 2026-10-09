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

public class CreatePendingMessageHandlerTests
{
    private const string Content = "dGVzdC1zdHJpbmc=";

    [Fact]
    public async Task A_message_without_uuid_is_stored_with_a_new_one()
    {
        await using var node = await TestNode.StartAsync();

        var result = await node.Send(new CreatePendingMessageCommand(TestKeys.Address2, Content, null));

        Assert.True(result.Success);
        Assert.True(Guid.TryParse(result.Value!.Uuid, out _));
        Assert.Equal(TestKeys.Address2, result.Value.Destination);
        Assert.Equal(Content, result.Value.Content);
        Assert.False(result.Value.Sent);
        Assert.True(result.Value.Id > 0);
        Assert.Equal(1, await node.Database(context => context.Messages.CountAsync()));
    }

    [Fact]
    public async Task A_message_with_a_uuid_is_stored_under_that_uuid()
    {
        await using var node = await TestNode.StartAsync();
        var uuid = Guid.NewGuid().ToString();

        var result = await node.Send(new CreatePendingMessageCommand(TestKeys.Address2, Content, uuid));

        Assert.True(result.Success);
        Assert.Equal(uuid, result.Value!.Uuid);
    }

    [Fact]
    public async Task A_second_message_with_the_same_uuid_is_not_stored_and_the_stored_one_is_returned_with_a_failure()
    {
        await using var node = await TestNode.StartAsync();
        var uuid = Guid.NewGuid().ToString();
        var first = await node.Send(new CreatePendingMessageCommand(TestKeys.Address2, Content, uuid));

        var second = await node.Send(new CreatePendingMessageCommand(TestKeys.Address3, Content, uuid));

        Assert.False(second.Success);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
        Assert.Equal(TestKeys.Address2, second.Value.Destination);
        Assert.Equal(1, await node.Database(context => context.Messages.CountAsync()));
    }

    [Fact]
    public async Task Two_messages_without_uuid_and_with_the_same_content_are_both_stored()
    {
        await using var node = await TestNode.StartAsync();

        await node.Send(new CreatePendingMessageCommand(TestKeys.Address2, Content, null));
        await node.Send(new CreatePendingMessageCommand(TestKeys.Address2, Content, null));

        Assert.Equal(2, await node.Database(context => context.Messages.CountAsync()));
    }

    [Theory]
    [InlineData("not-an-address", Content)]
    [InlineData("cbff2e12fb1f752cb17185f080f2b40301165a1051531cc0614e495ee2620ef9", "not base64!")]
    public async Task A_message_with_a_bad_destination_or_content_is_refused(string destination, string content)
    {
        await using var node = await TestNode.StartAsync();

        var result = await node.Send(new CreatePendingMessageCommand(destination, content, null));

        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(0, await node.Database(context => context.Messages.CountAsync()));
    }
}
