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
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Enigma5.App.Tests.Resources.Handlers;

public class IncrementFileAccessCountHandlerTests
{
    [Fact]
    public async Task Each_call_counts_one_access_and_the_file_is_deleted_at_its_maximum()
    {
        await using var node = await TestNode.StartAsync();
        var tag = await Uploads.Upload(node, maxAccessCount: 2);
        var path = Path.Combine(node.UploadFolder, tag);

        var first = await node.Send(new IncrementFileAccessCountCommand(tag));
        var existsAfterFirst = File.Exists(path);
        var second = await node.Send(new IncrementFileAccessCountCommand(tag));
        var third = await node.Send(new IncrementFileAccessCountCommand(tag));

        Assert.Equal((true, 1), (first.Success, first.Value));
        Assert.True(existsAfterFirst);
        Assert.Equal((true, 1), (second.Success, second.Value));
        Assert.False(File.Exists(path));
        Assert.Equal(0, await node.Database(context => context.Files.CountAsync()));
        Assert.Equal((true, 0), (third.Success, third.Value));
    }

    [Fact]
    public async Task An_unknown_tag_changes_no_record()
    {
        await using var node = await TestNode.StartAsync();

        var result = await node.Send(new IncrementFileAccessCountCommand(Guid.NewGuid().ToString()));

        Assert.Equal((true, 0), (result.Success, result.Value));
    }
}
