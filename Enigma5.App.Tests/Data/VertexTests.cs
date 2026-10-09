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

public class VertexTests
{
    private readonly FixedKeyCertificateManager _key1 = FixedKeyCertificateManager.Key1();

    private readonly NetworkGraphValidationPolicy _policy = new(TestConfiguration.Create());

    #region Factory

    [Fact]
    public async Task CreateAsync_signs_a_vertex_for_the_address_of_the_key()
    {
        var before = DateTimeOffset.UtcNow;

        var vertex = await Vertex.Factory.CreateAsync(_key1, [TestKeys.Address2], "http://node.example", "http://example.onion");

        Assert.NotNull(vertex);
        Assert.Equal(TestKeys.PublicKey1, vertex.PublicKey);
        Assert.Equal(TestKeys.Address1, vertex.Neighborhood.Address);
        Assert.Equal([TestKeys.Address2], vertex.Neighborhood.Neighbors);
        Assert.Equal("http://node.example", vertex.Neighborhood.Hostname);
        Assert.Equal("http://example.onion", vertex.Neighborhood.OnionService);
        Assert.InRange(vertex.Neighborhood.LastUpdate!.Value, before, DateTimeOffset.UtcNow);
        Assert.True(_policy.Validate(vertex));
    }

    [Fact]
    public async Task CreateAsync_without_neighbors_gives_an_empty_list()
    {
        var vertex = await Vertex.Factory.CreateAsync(_key1);

        Assert.NotNull(vertex);
        Assert.Empty(vertex.Neighborhood.Neighbors);
        Assert.True(_policy.Validate(vertex));
    }

    [Fact]
    public async Task CreateAsync_returns_null_when_the_key_cannot_sign()
    {
        _key1.Locked = true;

        Assert.Null(await Vertex.Factory.CreateAsync(_key1, [TestKeys.Address2]));
    }

    [Fact]
    public async Task CreateAsync_from_a_vertex_keeps_its_neighbors_and_names()
    {
        var first = await Vertex.Factory.CreateAsync(_key1, [TestKeys.Address2], "http://node.example", "http://example.onion");

        var second = await Vertex.Factory.CreateAsync(_key1, first!);

        Assert.NotNull(second);
        Assert.True(first!.Neighborhood == second.Neighborhood);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Create_gives_an_unsigned_vertex_without_neighbors()
    {
        var vertex = Vertex.Factory.Create(TestKeys.Address1);

        Assert.Equal(TestKeys.Address1, vertex.Neighborhood.Address);
        Assert.Empty(vertex.Neighborhood.Neighbors);
        Assert.Null(vertex.PublicKey);
        Assert.Null(vertex.SignedData);
        Assert.False(_policy.Validate(vertex));
    }

    #endregion

    #region Adding and removing neighbors

    [Fact]
    public async Task AddNeighborsAsync_returns_a_new_signed_vertex_with_the_neighbors_added()
    {
        var vertex = await Vertex.Factory.CreateAsync(_key1, [TestKeys.Address2], "http://node.example");

        var changed = await Vertex.Factory.Prototype.AddNeighborsAsync(vertex!, [TestKeys.Address3], _key1);

        Assert.NotNull(changed);
        Assert.Equal([TestKeys.Address2, TestKeys.Address3], changed.Neighborhood.Neighbors.Order());
        Assert.Equal("http://node.example", changed.Neighborhood.Hostname);
        Assert.Equal([TestKeys.Address2], vertex!.Neighborhood.Neighbors);
        Assert.True(_policy.Validate(changed));
    }

    [Fact]
    public async Task AddNeighborsAsync_returns_null_when_nothing_would_change()
    {
        var vertex = await Vertex.Factory.CreateAsync(_key1, [TestKeys.Address2]);

        Assert.Null(await Vertex.Factory.Prototype.AddNeighborsAsync(vertex!, [TestKeys.Address2], _key1));
        Assert.Null(await Vertex.Factory.Prototype.AddNeighborsAsync(vertex!, [], _key1));
    }

    [Fact]
    public async Task AddNeighborAsync_takes_the_address_of_another_vertex()
    {
        var vertex = await Vertex.Factory.CreateAsync(_key1);
        var other = await Vertex.Factory.CreateAsync(FixedKeyCertificateManager.Key3());

        var changed = await Vertex.Factory.Prototype.AddNeighborAsync(vertex!, other!, _key1);

        Assert.Equal([TestKeys.Address3], changed!.Neighborhood.Neighbors);
    }

    [Fact]
    public async Task AddNeighborAsync_returns_null_for_a_vertex_without_address()
    {
        var vertex = await Vertex.Factory.CreateAsync(_key1);

        Assert.Null(await Vertex.Factory.Prototype.AddNeighborAsync(vertex!, Vertex.Factory.Create(null), _key1));
    }

    [Fact]
    public async Task RemoveNeighborsAsync_returns_a_new_signed_vertex_without_the_neighbors()
    {
        var vertex = await Vertex.Factory.CreateAsync(_key1, [TestKeys.Address2, TestKeys.Address3]);

        var changed = await Vertex.Factory.Prototype.RemoveNeighborAsync(vertex!, TestKeys.Address2, _key1);

        Assert.NotNull(changed);
        Assert.Equal([TestKeys.Address3], changed.Neighborhood.Neighbors);
        Assert.True(_policy.Validate(changed));
    }

    [Fact]
    public async Task RemoveNeighborsAsync_returns_null_when_nothing_would_change()
    {
        var vertex = await Vertex.Factory.CreateAsync(_key1, [TestKeys.Address2]);

        Assert.Null(await Vertex.Factory.Prototype.RemoveNeighborAsync(vertex!, TestKeys.Address3, _key1));
    }

    [Fact]
    public async Task A_change_of_neighbors_returns_null_when_the_key_cannot_sign()
    {
        var vertex = await Vertex.Factory.CreateAsync(_key1, [TestKeys.Address2]);
        _key1.Locked = true;

        Assert.Null(await Vertex.Factory.Prototype.AddNeighborAsync(vertex!, TestKeys.Address3, _key1));
        Assert.Null(await Vertex.Factory.Prototype.RemoveNeighborAsync(vertex!, TestKeys.Address2, _key1));
    }

    #endregion

    #region Equality

    [Fact]
    public async Task Two_vertices_are_equal_when_their_addresses_are_equal()
    {
        var first = await Vertex.Factory.CreateAsync(_key1, [TestKeys.Address2]);
        var second = await Vertex.Factory.CreateAsync(_key1, [TestKeys.Address3]);
        var other = await Vertex.Factory.CreateAsync(FixedKeyCertificateManager.Key3());

        Assert.True(first == second);
        Assert.Equal(first!.GetHashCode(), second!.GetHashCode());
        Assert.True(first != other);
        Assert.Single(new HashSet<Vertex> { first, second });
    }

    #endregion
}
