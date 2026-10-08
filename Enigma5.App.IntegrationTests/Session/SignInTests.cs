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
using Enigma5.Tests.Base;

namespace Enigma5.App.IntegrationTests.Session;

public class SignInTests(NodeAFixture fixture) : IClassFixture<NodeAFixture>
{
    private readonly NodeProcess _node = fixture.Node;

    [Fact]
    public async Task A_client_that_signs_the_challenge_is_signed_in()
    {
        await using var client = await HubClient.ConnectAsync(_node.PublicUrl);

        var result = await client.SignInAsync(TestKeys.PrivateKey3);

        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.True((await client.Pull2()).Success);
    }

    [Fact]
    public async Task A_call_that_needs_a_session_is_refused_before_sign_in()
    {
        await using var client = await HubClient.ConnectAsync(_node.PublicUrl);

        var result = await client.Pull2();

        Assert.False(result.Success);
        Assert.Equal("Authentication required", result.Error);
    }

    [Fact]
    public async Task A_signature_made_with_another_key_is_refused()
    {
        await using var client = await HubClient.ConnectAsync(_node.PublicUrl);
        var challenge = await client.Invoke<string>("GenerateToken");

        var result = await client.Invoke<bool>("Authenticate", new AuthenticationRequestDto(TestKeys.PublicKey3, TestSignatures.SignChallenge(TestKeys.PrivateKey2, challenge.Data!)));

        Assert.False(result.Success);
        Assert.False((await client.Pull2()).Success);
    }

    [Fact]
    public async Task A_challenge_cannot_be_used_twice()
    {
        await using var client = await HubClient.ConnectAsync(_node.PublicUrl);
        var challenge = await client.Invoke<string>("GenerateToken");
        var request = new AuthenticationRequestDto(TestKeys.PublicKey3, TestSignatures.SignChallenge(TestKeys.PrivateKey3, challenge.Data!));

        Assert.True((await client.Invoke<bool>("Authenticate", request)).Success);
        Assert.False((await client.Invoke<bool>("Authenticate", request)).Success);
    }

    [Fact]
    public async Task A_malformed_request_is_answered_with_the_rule_it_breaks()
    {
        await using var client = await HubClient.ConnectAsync(_node.PublicUrl);

        var result = await client.Invoke<bool>("Authenticate", new AuthenticationRequestDto("not a key", "not base64!"));

        Assert.False(result.Success);
        Assert.Equal("One or more properties not in correct format.", result.Error);
    }

    [Fact]
    public async Task A_second_connection_with_the_same_key_takes_the_session_over()
    {
        await using var first = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey3);
        await using var second = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey3);

        Assert.False((await first.Pull2()).Success);
        Assert.True((await second.Pull2()).Success);
    }

    [Fact]
    public async Task The_node_gives_its_local_vertex_without_sign_in()
    {
        await using var client = await HubClient.ConnectAsync(_node.PublicUrl);

        var result = await client.Invoke<VertexDto>("GetLocalVertex");

        Assert.True(result.Success);
        Assert.Equal(TestKeys.Address1, result.Data!.Neighborhood!.Address);
    }
}
