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

internal static class Uploads
{
    public static readonly byte[] Content = [.. Enumerable.Range(0, 5000).Select(index => (byte)(index % 251))];

    public static IFormFile File(byte[]? content = null)
    {
        content ??= Content;
        return new FormFile(new MemoryStream(content), 0, content.Length, "file", "upload.bin");
    }

    public static async Task<string> Upload(TestNode node, int maxAccessCount = 1)
    => (await node.Send(new CreateFileCommand(File(), maxAccessCount))).Value!.Tag!;
}

public class CreateFileHandlerTests
{
    [Fact]
    public async Task An_uploaded_file_is_stored_under_its_tag_with_a_record()
    {
        await using var node = await TestNode.StartAsync(("FilesRetentionPeriod", "02.00:00:00"));
        var before = DateTimeOffset.UtcNow;

        var result = await node.Send(new CreateFileCommand(Uploads.File(), 4));

        Assert.True(result.Success);
        var record = await node.Database(context => context.Files.SingleAsync());
        Assert.Equal(record.Tag, result.Value!.Tag);
        Assert.Equal(4, record.MaxAccessCount);
        Assert.Equal(0, record.AccessCount);
        Assert.Equal($"http://node.example/File?Tag={record.Tag}", result.Value.ResourceUrl);
        Assert.InRange(result.Value.ValidUntil!.Value, before.AddDays(2), DateTimeOffset.UtcNow.AddDays(2));
        Assert.Equal(Uploads.Content, File.ReadAllBytes(Path.Combine(node.UploadFolder, record.Tag)));
    }

    [Fact]
    public async Task The_upload_folder_is_created_when_it_does_not_exist()
    {
        await using var node = await TestNode.StartAsync();
        Directory.Delete(node.UploadFolder, recursive: true);

        var result = await node.Send(new CreateFileCommand(Uploads.File(), 1));

        Assert.True(result.Success);
        Assert.Single(Directory.GetFiles(node.UploadFolder));
    }

    [Fact]
    public async Task An_empty_file_is_refused()
    {
        await using var node = await TestNode.StartAsync();

        var result = await node.Send(new CreateFileCommand(Uploads.File([]), 1));

        Assert.False(result.Success);
        Assert.Equal(0, await node.Database(context => context.Files.CountAsync()));
        Assert.Empty(Directory.GetFiles(node.UploadFolder));
    }

    // Without this, the record would promise a file that does not exist.
    [Fact]
    public async Task When_the_file_cannot_be_written_its_record_and_the_partial_file_are_removed()
    {
        await using var node = await TestNode.StartAsync();
        var failing = Substitute.For<IFormFile>();
        failing.Length.Returns(100);
        failing.CopyToAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            await call.Arg<Stream>().WriteAsync(new byte[10]);
            throw new IOException("The disk is full.");
        });

        var result = await node.Send(new CreateFileCommand(failing, 1));

        Assert.False(result.Success);
        Assert.Equal(0, await node.Database(context => context.Files.CountAsync()));
        Assert.Empty(Directory.GetFiles(node.UploadFolder));
    }
}
