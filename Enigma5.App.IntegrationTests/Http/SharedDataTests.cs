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
using System.Text;
using System.Text.Json;
using Enigma5.Tests.Base;

namespace Enigma5.App.IntegrationTests.Http;

public class SharedDataTests(SmallLimitsNodeFixture fixture) : IClassFixture<SmallLimitsNodeFixture>
{
    private readonly NodeProcess _node = fixture.Node;

    private Task<HttpResponseMessage> Post(string? publicKey, string? signedData, int? accessCount = 1)
    => _node.Public.PostAsJsonAsync("/Share", new { publicKey, signedData, accessCount });

    private async Task<string> Create(int accessCount = 1)
    {
        var response = await Post(TestKeys.PublicKey3, TestSignedData.SharedPayloadSignedWithKey3, accessCount);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tag").GetString()!;
    }

    [Fact]
    public async Task Created_data_is_returned_with_its_tag_its_address_on_the_node_and_its_end_of_life()
    {
        var response = await Post(TestKeys.PublicKey3, TestSignedData.SharedPayloadSignedWithKey3);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var tag = created.GetProperty("tag").GetString()!;
        Assert.True(Guid.TryParse(tag, out _));
        Assert.Equal($"{_node.PublicUrl}/Share?Tag={tag}", created.GetProperty("resourceUrl").GetString());
        Assert.True(created.GetProperty("validUntil").GetDateTimeOffset() > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Stored_data_is_read_by_its_tag_in_any_letter_case()
    {
        var tag = await Create();

        var response = await _node.Public.GetAsync($"/Share?tag={tag.ToUpperInvariant()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var read = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(tag, read.GetProperty("tag").GetString());
        Assert.Equal(TestSignedData.SharedPayloadSignedWithKey3, read.GetProperty("data").GetString());
        Assert.Equal(TestKeys.PublicKey3, read.GetProperty("publicKey").GetString());
    }

    [Fact]
    public async Task Reading_does_not_use_up_the_access_count()
    {
        var tag = await Create();

        Assert.Equal(HttpStatusCode.OK, (await _node.Public.GetAsync($"/Share?tag={tag}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _node.Public.GetAsync($"/Share?tag={tag}")).StatusCode);
    }

    [Fact]
    public async Task Data_is_gone_when_its_access_count_is_reached()
    {
        var tag = await Create(accessCount: 2);

        Assert.Equal(HttpStatusCode.OK, (await _node.Public.PutAsync($"/IncrementSharedDataAccessCount?tag={tag}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _node.Public.GetAsync($"/Share?tag={tag}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _node.Public.PutAsync($"/IncrementSharedDataAccessCount?tag={tag}", null)).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await _node.Public.GetAsync($"/Share?tag={tag}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _node.Public.PutAsync($"/IncrementSharedDataAccessCount?tag={tag}", null)).StatusCode);
        Assert.Equal(0, _node.Count("SELECT COUNT(*) FROM SharedData WHERE Tag = $t", ("$t", tag)));
    }

    [Fact]
    public async Task Counting_at_once_from_many_callers_never_goes_past_the_access_count()
    {
        var tag = await Create(accessCount: 3);

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => _node.Public.PutAsync($"/IncrementSharedDataAccessCount?tag={tag}", null)));

        Assert.Equal(3, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(7, responses.Count(response => response.StatusCode == HttpStatusCode.NotFound));
    }

    [Theory]
    [InlineData("")]
    [InlineData("?tag=")]
    [InlineData("?tag=abc")]
    public async Task A_tag_that_is_missing_or_not_a_guid_is_a_bad_request(string query)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _node.Public.GetAsync($"/Share{query}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _node.Public.PutAsync($"/IncrementSharedDataAccessCount{query}", null)).StatusCode);
    }

    [Fact]
    public async Task An_unknown_tag_is_not_found()
    {
        var tag = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await _node.Public.GetAsync($"/Share?tag={tag}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _node.Public.PutAsync($"/IncrementSharedDataAccessCount?tag={tag}", null)).StatusCode);
    }

    [Fact]
    public async Task Data_signed_with_another_key_is_refused()
    {
        var response = await Post(TestKeys.PublicKey2, TestSignedData.SharedPayloadSignedWithKey3);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("The signature could not be verified.", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task An_access_count_below_one_is_refused(int accessCount)
    {
        var response = await Post(TestKeys.PublicKey3, TestSignedData.SharedPayloadSignedWithKey3, accessCount);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_request_without_key_or_data_is_refused()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(null, TestSignedData.SharedPayloadSignedWithKey3)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(TestKeys.PublicKey3, null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post("not a key", "not base64 !")).StatusCode);
    }

    [Fact]
    public async Task A_body_that_is_not_json_is_refused()
    {
        var response = await _node.Public.PostAsync("/Share", new StringContent("{ not json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_body_above_the_limit_of_the_node_is_refused_as_too_large()
    {
        var response = await Post(TestKeys.PublicKey3, new string('A', SmallLimitsNodeFixture.SharedDataMaxSize));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }
}
