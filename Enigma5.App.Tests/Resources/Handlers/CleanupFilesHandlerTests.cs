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

public class CleanupFilesHandlerTests
{
    // Makes a stored record look as if it had been created that long ago.
    private static Task Age(TestNode node, string tag, TimeSpan age)
    {
        var timestamp = (DateTimeOffset.UtcNow - age).ToUnixTimeSeconds();
        return node.Database(context => context.Files.Where(file => file.Tag == tag)
            .ExecuteUpdateAsync(setters => setters.SetProperty(file => file.Timestamp, timestamp)));
    }

    [Fact]
    public async Task Files_older_than_the_retention_period_are_deleted_with_their_records()
    {
        await using var node = await TestNode.StartAsync();
        var old = await Uploads.Upload(node);
        var recent = await Uploads.Upload(node);
        await Age(node, old, TimeSpan.FromDays(4));

        var result = await node.Send(new CleanupFilesCommand(TimeSpan.FromDays(3)));

        Assert.True(result.Success);
        Assert.Equal(1, result.Value);
        Assert.Equal([recent], await node.Database(context => context.Files.Select(file => file.Tag).ToListAsync()));
        Assert.Equal([recent], Directory.GetFiles(node.UploadFolder).Select(Path.GetFileName));
    }

    [Fact]
    public async Task An_old_record_whose_file_is_already_gone_is_removed_too()
    {
        await using var node = await TestNode.StartAsync();
        var tag = await Uploads.Upload(node);
        await Age(node, tag, TimeSpan.FromDays(4));
        File.Delete(Path.Combine(node.UploadFolder, tag));

        var result = await node.Send(new CleanupFilesCommand(TimeSpan.FromDays(3)));

        Assert.Equal(1, result.Value);
        Assert.Equal(0, await node.Database(context => context.Files.CountAsync()));
    }

    // Known limit: a file without a record is never cleaned up.
    [Fact]
    public async Task A_file_without_record_is_left_in_the_upload_folder()
    {
        await using var node = await TestNode.StartAsync();
        var stray = Path.Combine(node.UploadFolder, Guid.NewGuid().ToString());
        File.WriteAllBytes(stray, [1, 2, 3]);

        await node.Send(new CleanupFilesCommand(TimeSpan.Zero));

        Assert.True(File.Exists(stray));
    }
}
