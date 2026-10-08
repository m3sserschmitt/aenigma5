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
using Enigma5.Tests.Base;

namespace Enigma5.App.Tests.Data;

public class NetworkGraphValidationPolicyTests
{
    private readonly FixedKeyCertificateManager _key1 = FixedKeyCertificateManager.Key1();

    private readonly NetworkGraphValidationPolicy _policy = new(TestConfiguration.Create());

    private Task<Vertex?> SignedVertex(params string[] neighbors) => Vertex.Factory.CreateAsync(_key1, [.. neighbors], "http://node.example");

    [Fact]
    public async Task A_vertex_signed_by_its_own_key_is_valid()
    {
        Assert.True(_policy.Validate((await SignedVertex(TestKeys.Address2))!));
    }

    [Fact]
    public async Task A_vertex_older_than_the_lifetime_is_not_valid()
    {
        var vertex = (await SignedVertex())!;
        var policy = new NetworkGraphValidationPolicy(TestConfiguration.Create(("VertexLifetime", "00:00:00")));

        await Task.Delay(20);

        Assert.False(policy.IsNotExpired(vertex));
        Assert.False(policy.Validate(vertex));
    }

    [Fact]
    public async Task A_vertex_that_lists_itself_is_not_valid()
    {
        var vertex = (await SignedVertex(TestKeys.Address1))!;

        Assert.False(NetworkGraphValidationPolicy.HasNoCycles(vertex));
        Assert.False(_policy.Validate(vertex));
    }

    [Fact]
    public async Task A_vertex_with_a_neighbor_that_is_not_an_address_is_not_valid()
    {
        var vertex = (await SignedVertex(TestKeys.Address2, "not-an-address"))!;

        Assert.False(NetworkGraphValidationPolicy.ValidateNeighborsAddresses(vertex));
        Assert.False(_policy.Validate(vertex));
    }

    [Fact]
    public async Task A_vertex_whose_address_does_not_belong_to_its_public_key_is_not_valid()
    {
        var signed = (await SignedVertex())!;
        var vertex = new Vertex(signed.Neighborhood, TestKeys.PublicKey2, signed.SignedData);

        Assert.False(NetworkGraphValidationPolicy.ValidateAddress(vertex));
        Assert.False(_policy.Validate(vertex));
    }

    [Fact]
    public async Task A_vertex_whose_content_differs_from_the_signed_content_is_not_valid()
    {
        var signed = (await SignedVertex(TestKeys.Address2))!;
        var changed = new Neighborhood([TestKeys.Address2, TestKeys.Address3], signed.Neighborhood.Address, signed.Neighborhood.Hostname, null, signed.Neighborhood.LastUpdate);
        var vertex = new Vertex(changed, signed.PublicKey, signed.SignedData);

        Assert.False(NetworkGraphValidationPolicy.ValidateSignature(vertex));
        Assert.False(_policy.Validate(vertex));
    }

    [Fact]
    public async Task A_vertex_whose_signature_was_changed_is_not_valid()
    {
        var signed = (await SignedVertex())!;
        var bytes = Convert.FromBase64String(signed.SignedData!);
        bytes[^1] ^= 0xFF;
        var vertex = new Vertex(signed.Neighborhood, signed.PublicKey, Convert.ToBase64String(bytes));

        Assert.False(NetworkGraphValidationPolicy.ValidateSignature(vertex));
        Assert.False(_policy.Validate(vertex));
    }

    [Fact]
    public async Task A_vertex_without_signature_or_public_key_is_not_valid()
    {
        var signed = (await SignedVertex())!;

        Assert.False(_policy.Validate(new Vertex(signed.Neighborhood, signed.PublicKey, null)));
        Assert.False(_policy.Validate(new Vertex(signed.Neighborhood, null, signed.SignedData)));
        Assert.False(_policy.Validate(new Vertex(signed.Neighborhood, signed.PublicKey, "not base64!")));
        Assert.False(_policy.Validate(Vertex.Factory.Create(TestKeys.Address1)));
    }
}
