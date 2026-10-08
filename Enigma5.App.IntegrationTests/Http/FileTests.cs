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

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Enigma5.App.IntegrationTests.Http;

public class FileTests(SmallLimitsNodeFixture fixture) : IClassFixture<SmallLimitsNodeFixture>
{
    private readonly NodeProcess _node = fixture.Node;

    private static readonly byte[] Content = [.. Enumerable.Range(0, 1000).Select(index => (byte)(index % 251))];

    private Task<HttpResponseMessage> Post(byte[]? content, string? maxAccessCount)
    {
        var form = new MultipartFormDataContent();
        if (content is not null)
        {
            form.Add(new ByteArrayContent(content), "file", "upload.bin");
        }
        if (maxAccessCount is not null)
        {
            form.Add(new StringContent(maxAccessCount), "maxAccessCount");
        }
        return _node.Public.PostAsync("/File", form);
    }

    private async Task<string> Upload(int maxAccessCount = 1)
    {
        var response = await Post(Content, maxAccessCount.ToString());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tag").GetString()!;
    }

    private int StoredFiles() => Directory.Exists(_node.UploadFolder) ? Directory.GetFiles(_node.UploadFolder, "*", SearchOption.AllDirectories).Length : 0;

    [Fact]
    public async Task An_uploaded_file_is_returned_with_its_tag_and_its_address_on_the_node()
    {
        var response = await Post(Content, "1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var tag = created.GetProperty("tag").GetString()!;
        Assert.True(Guid.TryParse(tag, out _));
        Assert.Equal($"{_node.PublicUrl}/File?Tag={tag}", created.GetProperty("resourceUrl").GetString());
    }

    [Fact]
    public async Task A_stored_file_is_downloaded_unchanged_by_its_tag_in_any_letter_case()
    {
        var tag = await Upload();

        var response = await _node.Public.GetAsync($"/File?tag={tag.ToUpperInvariant()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(tag, response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.Equal(Content, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Downloading_does_not_use_up_the_access_count()
    {
        var tag = await Upload();

        Assert.Equal(HttpStatusCode.OK, (await _node.Public.GetAsync($"/File?tag={tag}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _node.Public.GetAsync($"/File?tag={tag}")).StatusCode);
    }

    [Fact]
    public async Task A_file_is_gone_from_the_node_and_its_folder_when_its_access_count_is_reached()
    {
        var before = StoredFiles();
        var tag = await Upload(maxAccessCount: 2);
        Assert.Equal(before + 1, StoredFiles());

        Assert.Equal(HttpStatusCode.OK, (await _node.Public.PutAsync($"/IncrementFileAccessCount?tag={tag}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _node.Public.GetAsync($"/File?tag={tag}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _node.Public.PutAsync($"/IncrementFileAccessCount?tag={tag}", null)).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await _node.Public.GetAsync($"/File?tag={tag}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _node.Public.PutAsync($"/IncrementFileAccessCount?tag={tag}", null)).StatusCode);
        Assert.Equal(before, StoredFiles());
    }

    [Fact]
    public async Task Counting_at_once_from_many_callers_never_goes_past_the_access_count()
    {
        var tag = await Upload(maxAccessCount: 3);

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => _node.Public.PutAsync($"/IncrementFileAccessCount?tag={tag}", null)));

        Assert.Equal(3, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(7, responses.Count(response => response.StatusCode == HttpStatusCode.NotFound));
    }

    [Theory]
    [InlineData("")]
    [InlineData("?tag=")]
    [InlineData("?tag=abc")]
    public async Task A_tag_that_is_missing_or_not_a_guid_is_a_bad_request(string query)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _node.Public.GetAsync($"/File{query}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _node.Public.PutAsync($"/IncrementFileAccessCount{query}", null)).StatusCode);
    }

    [Fact]
    public async Task An_unknown_tag_is_not_found()
    {
        var tag = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await _node.Public.GetAsync($"/File?tag={tag}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _node.Public.PutAsync($"/IncrementFileAccessCount?tag={tag}", null)).StatusCode);
    }

    [Fact]
    public async Task An_upload_without_file_or_with_an_empty_file_is_refused()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(null, "1")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post([], "1")).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("many")]
    public async Task An_upload_without_a_count_of_at_least_one_is_refused_and_nothing_is_stored(string? maxAccessCount)
    {
        var before = StoredFiles();

        var response = await Post(Content, maxAccessCount);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, StoredFiles());
    }

    [Fact]
    public async Task A_file_above_the_limit_of_the_node_is_refused_and_nothing_is_stored()
    {
        var before = StoredFiles();

        var response = await Post(new byte[SmallLimitsNodeFixture.SharedFileMaxSize + 1], "1");

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(before, StoredFiles());
    }
}
