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
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Handlers;
using Enigma5.App.Resources.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Enigma5.App.Tests.Resources.Handlers;

public class GetFileHandlerTests
{
    // Not found is a successful lookup without a value.
    private static void AssertNotFound(CommandResult<SharedDataDto> result)
    {
        Assert.True(result.Success);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task A_stored_file_is_returned_as_a_stream()
    {
        await using var node = await TestNode.StartAsync();
        var tag = await Uploads.Upload(node);

        var result = await node.Send(new GetFileQuery(tag));

        Assert.True(result.Success);
        Assert.Equal(tag, result.Value!.Tag);
        await using var stream = result.Value.File!;
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        Assert.Equal(Uploads.Content, copy.ToArray());
    }

    [Fact]
    public async Task An_unknown_tag_is_a_successful_lookup_without_a_value()
    {
        await using var node = await TestNode.StartAsync();

        AssertNotFound(await node.Send(new GetFileQuery(Guid.NewGuid().ToString())));
    }

    [Fact]
    public async Task A_record_whose_file_is_missing_is_not_found()
    {
        await using var node = await TestNode.StartAsync();
        var tag = await Uploads.Upload(node);
        File.Delete(Path.Combine(node.UploadFolder, tag));

        AssertNotFound(await node.Send(new GetFileQuery(tag)));
    }

    [Fact]
    public async Task A_file_without_record_is_not_found()
    {
        await using var node = await TestNode.StartAsync();
        var tag = Guid.NewGuid().ToString();
        File.WriteAllBytes(Path.Combine(node.UploadFolder, tag), [1, 2, 3]);

        AssertNotFound(await node.Send(new GetFileQuery(tag)));
    }

    [Theory]
    [InlineData("../node.sqlite")]
    [InlineData("/etc/hostname")]
    [InlineData("not-a-guid")]
    public async Task A_tag_that_is_not_a_guid_never_reaches_a_file(string tag)
    {
        await using var node = await TestNode.StartAsync();

        AssertNotFound(await node.Send(new GetFileQuery(tag)));
    }
}
