/*
    Aenigma - Federal messaging system
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

using Autofac;
using Enigma5.App.Data;
using Enigma5.Security.Contracts;
using Enigma5.Crypto.DataProviders;
using Enigma5.Crypto.Contracts;

namespace Enigma5.App.Tests.Data;

public class VertexFactoryTests : AppTestBase
{
    private readonly ICertificateManager _certificateManager;

    private readonly IEnvelopeSigner _signer;

    public VertexFactoryTests()
    {
        _certificateManager = _scope.Resolve<ICertificateManager>();
        _signer = _scope.Resolve<IEnvelopeSigner>();
    }

    [Fact]
    public void ShouldCreateVertexWithEmptyAdjacencyList()
    {
        // Arrange

        // Act
        var vertex = Vertex.Factory.CreateWithEmptyNeighborhood(_signer, _certificateManager, "localhost");

        // Assert
        vertex.Should().NotBeNull();
        vertex.Should().BeOfType<Vertex>();
        vertex!.PublicKey.Should().Be(_certificateManager.PublicKey);
        vertex.SignedData.Should().NotBeNullOrEmpty();
        vertex.Neighborhood.Address.Should().Be(_certificateManager.Address);
        vertex.Neighborhood.Hostname.Should().Be("localhost");
        vertex.Neighborhood.Neighbors.Should().BeEmpty();
    }

    [Fact]
    public void ShouldAddNeighbor()
    {
        // Arrange
        var vertex = _scope.ResolveLocalVertex();

        // Act
        var added = Vertex.Factory.Prototype.AddNeighbor(vertex, PKey.Address1, _signer, _certificateManager, out Vertex? newVertex);

        // Assert
        added.Should().BeTrue();
        newVertex.Should().BeOfType<Vertex>();
        newVertex.Should().NotBeNull();
        newVertex.Should().NotBeSameAs(vertex);
        newVertex!.PublicKey.Should().Be(_certificateManager.PublicKey);
        newVertex.SignedData.Should().NotBeEmpty();
        newVertex.Neighborhood.Address.Should().Be(_certificateManager.Address);
        newVertex.Neighborhood.Neighbors.Should().Contain(PKey.Address1);
        newVertex.Neighborhood.Hostname.Should().Be("localhost");
    }

    [Fact]
    public void ShouldNotAddNeighborTwice()
    {
        // Arrange
        var vertex = _scope.ResolveLocalVertex([PKey.Address1]);

        // Act
        var secondAdded = Vertex.Factory.Prototype.AddNeighbor(vertex, PKey.Address1, _signer, _certificateManager, out Vertex? newVertex);

        // Assert
        secondAdded.Should().BeFalse();
        newVertex.Should().BeNull();
    }

    [Fact]
    public void ShouldRemoveNeighbor()
    {
        // Arrange
        var vertex = _scope.ResolveLocalVertex([PKey.Address1]);

        // Act
        var removed = Vertex.Factory.Prototype.RemoveNeighbor(vertex, PKey.Address1, _signer, _certificateManager, out Vertex? newVertex);

        // Assert
        removed.Should().BeTrue();
        newVertex.Should().NotBeNull();
        newVertex.Should().BeOfType<Vertex>();
        newVertex.Should().NotBeSameAs(vertex);
        newVertex!.PublicKey.Should().Be(_certificateManager.PublicKey);
        newVertex.SignedData.Should().NotBeEmpty();
        newVertex.Neighborhood.Address.Should().Be(_certificateManager.Address);
        newVertex.Neighborhood.Neighbors.Should().NotContain(PKey.Address1);
        newVertex.Neighborhood.Hostname.Should().Be("localhost");
    }

    [Fact]
    public void ShouldNotRemoveNonexistentNeighbor()
    {
        // Arrange
        var vertex = _scope.ResolveLocalVertex();

        // Act
        var removed = Vertex.Factory.Prototype.RemoveNeighbor(vertex, PKey.Address1, _signer, _certificateManager, out Vertex? newVertex);
        
        // Assert
        removed.Should().BeFalse();
        newVertex.Should().BeNull();
    }
}
