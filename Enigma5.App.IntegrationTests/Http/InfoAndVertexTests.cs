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
using Enigma5.Tests.Base;

namespace Enigma5.App.IntegrationTests.Http;

public class InfoAndVertexTests(NodeAFixture fixture) : IClassFixture<NodeAFixture>
{
    private readonly NodeProcess _node = fixture.Node;

    [Theory]
    [InlineData("/")]
    [InlineData("/Info")]
    public async Task Info_gives_the_key_the_address_and_the_graph_version(string path)
    {
        var info = await _node.Public.GetFromJsonAsync<JsonElement>(path);

        Assert.Equal(_node.PublicKey, info.GetProperty("publicKey").GetString());
        Assert.Equal(TestKeys.Address1, info.GetProperty("address").GetString());
        Assert.Equal(_node.PublicUrl, info.GetProperty("hostname").GetString());
        Assert.Equal(64, info.GetProperty("graphVersion").GetString()!.Length);
    }

    [Fact]
    public async Task LocalVertex_gives_the_signed_vertex_of_the_node()
    {
        var vertex = await _node.Public.GetFromJsonAsync<JsonElement>("/LocalVertex");

        Assert.Equal(TestKeys.Address1, vertex.GetProperty("neighborhood").GetProperty("address").GetString());
        Assert.False(string.IsNullOrEmpty(vertex.GetProperty("signedData").GetString()));
    }

    [Fact]
    public async Task Vertices_lists_the_graph()
    {
        var vertices = await _node.Public.GetFromJsonAsync<JsonElement>("/Vertices");

        Assert.Contains(vertices.EnumerateArray(), vertex => vertex.GetProperty("neighborhood").GetProperty("address").GetString() == TestKeys.Address1);
    }

    [Fact]
    public async Task Vertex_answers_200_for_a_known_address_also_in_upper_case()
    {
        Assert.Equal(HttpStatusCode.OK, (await _node.Public.GetAsync($"/Vertex?address={TestKeys.Address1}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _node.Public.GetAsync($"/Vertex?address={TestKeys.Address1.ToUpperInvariant()}")).StatusCode);
    }

    [Fact]
    public async Task Vertex_answers_404_for_an_unknown_address()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _node.Public.GetAsync($"/Vertex?address={TestKeys.Address3}")).StatusCode);
    }

    [Theory]
    [InlineData("/Vertex")]
    [InlineData("/Vertex?address=abc")]
    [InlineData("/Vertex?address=../../etc/passwd")]
    public async Task Vertex_answers_400_for_a_missing_or_malformed_address(string path)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _node.Public.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task An_unknown_path_answers_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _node.Public.GetAsync("/NoSuchEndpoint")).StatusCode);
    }
}
