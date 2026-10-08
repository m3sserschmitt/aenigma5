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

using Enigma5.App.Models;
using Enigma5.App.Resources.Handlers;
using Enigma5.App.Resources.Queries;
using MediatR;
using Enigma5.App.Tests.Resources.Handlers;
using Enigma5.Security.Contracts;
using Enigma5.Tests.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;

namespace Enigma5.App.Tests;

// The endpoint methods, called directly. What the web server adds (routes, size limits, reading the request)
// is covered by the integration tests.
public class ApiTests
{
    private static int? Status(IResult result) => Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode;

    private static T Value<T>(IResult result)
    {
        Assert.Equal(StatusCodes.Status200OK, Status(result));
        return Assert.IsType<T>(Assert.IsAssignableFrom<IValueHttpResult>(result).Value);
    }

    private static SharedDataCreateDto Share(int accessCount = 1) => new(TestKeys.PublicKey3, TestSignedData.SharedPayloadSignedWithKey3, accessCount);

    private static async Task<string> CreateShare(TestNode node, int accessCount = 1)
    => Value<SharedDataDto>(await node.Request(mediator => Api.PostShare(Share(accessCount), mediator))).Tag!;

    [Fact]
    public async Task GetInfo_returns_the_public_key_and_the_address_of_the_node()
    {
        await using var node = await TestNode.StartAsync();

        var info = Value<ServerInfoDto>(await node.Request(Api.GetInfo));

        Assert.Equal(TestKeys.PublicKey1, info.PublicKey);
        Assert.Equal(TestKeys.Address1, info.Address);
    }

    [Fact]
    public async Task PostShare_stores_the_data_and_returns_its_tag()
    {
        await using var node = await TestNode.StartAsync();

        var created = Value<SharedDataDto>(await node.Request(mediator => Api.PostShare(Share(), mediator)));

        Assert.True(Guid.TryParse(created.Tag, out _));
        Assert.Equal($"http://node.example/Share?Tag={created.Tag}", created.ResourceUrl);
    }

    [Fact]
    public async Task PostShare_refuses_a_request_that_is_not_valid_and_lists_the_errors()
    {
        await using var node = await TestNode.StartAsync();
        var signedByAnotherKey = new SharedDataCreateDto(TestKeys.PublicKey2, TestSignedData.SharedPayloadSignedWithKey3, 0);

        var result = await node.Request(mediator => Api.PostShare(signedByAnotherKey, mediator));

        Assert.Equal(StatusCodes.Status400BadRequest, Status(result));
        var errors = Assert.IsAssignableFrom<IEnumerable<ErrorDto>>(Assert.IsAssignableFrom<IValueHttpResult>(result).Value);
        Assert.Equal(2, errors.Count());
    }

    [Fact]
    public async Task GetShare_returns_stored_data_by_its_tag_in_any_letter_case()
    {
        await using var node = await TestNode.StartAsync();
        var tag = await CreateShare(node);

        var read = Value<SharedDataDto>(await node.Request(mediator => Api.GetShare($" {tag.ToUpperInvariant()} ", mediator)));

        Assert.Equal(tag, read.Tag);
        Assert.Equal(TestSignedData.SharedPayloadSignedWithKey3, read.Data);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    public async Task A_tag_that_is_missing_or_not_a_guid_is_a_bad_request_for_every_endpoint_with_a_tag(string? tag)
    {
        await using var node = await TestNode.StartAsync();

        Assert.Equal(StatusCodes.Status400BadRequest, Status(await node.Request(mediator => Api.GetShare(tag, mediator))));
        Assert.Equal(StatusCodes.Status400BadRequest, Status(await node.Request(mediator => Api.IncrementSharedDataAccessCount(tag, mediator))));
        Assert.Equal(StatusCodes.Status400BadRequest, Status(await node.Request(mediator => Api.GetFile(tag, mediator))));
        Assert.Equal(StatusCodes.Status400BadRequest, Status(await node.Request(mediator => Api.IncrementFileAccessCount(tag, mediator))));
    }

    [Fact]
    public async Task An_unknown_tag_is_not_found_for_every_endpoint_with_a_tag()
    {
        await using var node = await TestNode.StartAsync();
        var tag = Guid.NewGuid().ToString();

        Assert.Equal(StatusCodes.Status404NotFound, Status(await node.Request(mediator => Api.GetShare(tag, mediator))));
        Assert.Equal(StatusCodes.Status404NotFound, Status(await node.Request(mediator => Api.IncrementSharedDataAccessCount(tag, mediator))));
        Assert.Equal(StatusCodes.Status404NotFound, Status(await node.Request(mediator => Api.GetFile(tag, mediator))));
        Assert.Equal(StatusCodes.Status404NotFound, Status(await node.Request(mediator => Api.IncrementFileAccessCount(tag, mediator))));
    }

    // Not found is a successful lookup without a value. A lookup that fails is an error of the node, not a missing item.
    [Fact]
    public async Task GetShare_and_GetFile_are_server_errors_when_the_lookup_fails()
    {
        var failing = Substitute.For<IMediator>();
        failing.Send(Arg.Any<GetSharedDataQuery>(), Arg.Any<CancellationToken>()).Returns(CommandResult.CreateResultFailure<SharedDataDto>());
        failing.Send(Arg.Any<GetFileQuery>(), Arg.Any<CancellationToken>()).Returns(CommandResult.CreateResultFailure<SharedDataDto>());
        var tag = Guid.NewGuid().ToString();

        Assert.Equal(StatusCodes.Status500InternalServerError, Status(await Api.GetShare(tag, failing)));
        Assert.Equal(StatusCodes.Status500InternalServerError, Status(await Api.GetFile(tag, failing)));
    }

    [Fact]
    public async Task IncrementSharedDataAccessCount_is_ok_until_the_count_is_reached_and_not_found_afterwards()
    {
        await using var node = await TestNode.StartAsync();
        var tag = await CreateShare(node, accessCount: 2);

        Assert.Equal(StatusCodes.Status200OK, Status(await node.Request(mediator => Api.IncrementSharedDataAccessCount(tag, mediator))));
        Assert.Equal(StatusCodes.Status200OK, Status(await node.Request(mediator => Api.IncrementSharedDataAccessCount(tag, mediator))));
        Assert.Equal(StatusCodes.Status404NotFound, Status(await node.Request(mediator => Api.IncrementSharedDataAccessCount(tag, mediator))));
        Assert.Equal(StatusCodes.Status404NotFound, Status(await node.Request(mediator => Api.GetShare(tag, mediator))));
    }

    [Fact]
    public async Task GetVertex_returns_the_vertex_of_an_address_in_any_letter_case()
    {
        await using var node = await TestNode.StartAsync();

        var vertex = Value<VertexDto>(await node.Request(mediator => Api.GetVertex(TestKeys.Address1.ToUpperInvariant(), mediator)));

        Assert.Equal(TestKeys.Address1, vertex.Neighborhood!.Address);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    public async Task GetVertex_refuses_what_is_not_an_address(string? address)
    {
        await using var node = await TestNode.StartAsync();

        Assert.Equal(StatusCodes.Status400BadRequest, Status(await node.Request(mediator => Api.GetVertex(address, mediator))));
    }

    [Fact]
    public async Task GetVertex_does_not_find_an_address_that_is_not_in_the_graph()
    {
        await using var node = await TestNode.StartAsync();

        Assert.Equal(StatusCodes.Status404NotFound, Status(await node.Request(mediator => Api.GetVertex(TestKeys.Address2, mediator))));
    }

    [Fact]
    public async Task GetLocalVertex_returns_the_vertex_of_the_node()
    {
        await using var node = await TestNode.StartAsync();

        var vertex = Value<VertexDto>(await node.Request(mediator => Api.GetLocalVertex(mediator, node.Key)));

        Assert.Equal(TestKeys.Address1, vertex.Neighborhood!.Address);
        Assert.Equal(TestKeys.PublicKey1, vertex.PublicKey);
    }

    [Fact]
    public async Task GetLocalVertex_is_a_server_error_when_the_node_has_no_address()
    {
        await using var node = await TestNode.StartAsync();
        var withoutKey = Substitute.For<ICertificateManager>();
        withoutKey.GetAddressAsync().Returns((string?)null);

        Assert.Equal(StatusCodes.Status500InternalServerError, Status(await node.Request(mediator => Api.GetLocalVertex(mediator, withoutKey))));
    }

    [Fact]
    public async Task GetVertices_returns_the_whole_graph()
    {
        await using var node = await TestNode.StartAsync();

        var vertices = Value<List<VertexDto>>(await node.Request(Api.GetVertices));

        Assert.Equal(TestKeys.Address1, Assert.Single(vertices).Neighborhood!.Address);
    }

    [Fact]
    public async Task PostFile_stores_the_file_and_GetFile_returns_it_under_its_tag()
    {
        await using var node = await TestNode.StartAsync();

        var created = Value<SharedDataDto>(await node.Request(mediator => Api.PostFile(Uploads.File(), 1, mediator)));
        var download = Assert.IsType<FileStreamHttpResult>(await node.Request(mediator => Api.GetFile(created.Tag!.ToUpperInvariant(), mediator)));

        await using var stream = download.FileStream;
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        Assert.Equal(created.Tag, download.FileDownloadName);
        Assert.Equal(Uploads.Content, copy.ToArray());
    }

    [Fact]
    public async Task PostFile_refuses_an_upload_without_file_with_an_empty_file_or_without_a_count_of_at_least_one()
    {
        await using var node = await TestNode.StartAsync();

        Assert.Equal(StatusCodes.Status400BadRequest, Status(await node.Request(mediator => Api.PostFile(null, 1, mediator))));
        Assert.Equal(StatusCodes.Status400BadRequest, Status(await node.Request(mediator => Api.PostFile(Uploads.File([]), 1, mediator))));
        Assert.Equal(StatusCodes.Status400BadRequest, Status(await node.Request(mediator => Api.PostFile(Uploads.File(), null, mediator))));
        Assert.Equal(StatusCodes.Status400BadRequest, Status(await node.Request(mediator => Api.PostFile(Uploads.File(), 0, mediator))));
        Assert.Empty(Directory.GetFiles(node.UploadFolder));
    }

    [Fact]
    public async Task IncrementFileAccessCount_is_ok_until_the_count_is_reached_and_not_found_afterwards()
    {
        await using var node = await TestNode.StartAsync();
        var tag = await Uploads.Upload(node);

        Assert.Equal(StatusCodes.Status200OK, Status(await node.Request(mediator => Api.IncrementFileAccessCount(tag, mediator))));
        Assert.Equal(StatusCodes.Status404NotFound, Status(await node.Request(mediator => Api.IncrementFileAccessCount(tag, mediator))));
        Assert.Equal(StatusCodes.Status404NotFound, Status(await node.Request(mediator => Api.GetFile(tag, mediator))));
    }
}
