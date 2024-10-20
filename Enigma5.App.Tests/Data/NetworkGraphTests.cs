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

namespace Enigma5.App.Tests.Data;

public class NetworkGraphTests : AppTestBase
{
    private readonly ICertificateManager _certificateManager;

    private readonly NetworkGraph _graph;

    public NetworkGraphTests()
    {
        _certificateManager = _scope.Resolve<ICertificateManager>();
        _graph = _scope.Resolve<NetworkGraph>();
    }

    [Fact]
    public void NetworkGraph_ShouldAddNewVertex()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex();

        // Act
        var vertices = _graph.Update(vertex);

        // Assert
        vertices.Count.Should().Be(2);
        var localVertex = vertices.Single(item => item.Neighborhood.Address == _certificateManager.Address);
        localVertex.PublicKey.Should().Be(_certificateManager.PublicKey);
        localVertex.Neighborhood.Neighbors.Should().HaveCount(1);
        localVertex.Neighborhood.Neighbors.Single().Should().Be(vertex.Neighborhood.Address);
        var adjacentVertex = vertices.Single(item => item.Neighborhood.Address == vertex.Neighborhood.Address);
        adjacentVertex.Neighborhood.Neighbors.Should().HaveCount(1);
        adjacentVertex.Neighborhood.Neighbors.Should().Contain(_certificateManager.Address);
        _graph.LocalVertex.Neighborhood.Neighbors.Should().HaveCount(1);
        _graph.LocalVertex.Neighborhood.Neighbors.Should().Contain(vertex.Neighborhood.Address);
        _graph.Vertices.Should().HaveCount(2);
        _graph.Vertices.Should().Contain(_graph.LocalVertex);
        _graph.Vertices.Should().Contain(vertex);
    }

    [Fact]
    public void NetworkGraph_ShouldRemoveAdjacency()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex();

        var updatedVertex = _scope.ResolveNonAdjacentVertex();

        // Act
        var vertices = _graph.Update(vertex);
        var final = _graph.Update(updatedVertex);

        // Assert
        vertices.Count.Should().Be(2);
        final.Count.Should().Be(2);
        _graph.Vertices.Count.Should().Be(1);
        var localVertex = _graph.Vertices.Single(item => item.Neighborhood.Address == _certificateManager.Address);
        localVertex.Neighborhood.Neighbors.Should().BeEmpty();
        _graph.Vertices.Should().NotContain(item => item.Neighborhood.Address == vertex.Neighborhood.Address);
        _graph.Vertices.Should().Contain(item => item.Neighborhood.Address == _certificateManager.Address);
    }

    [Fact]
    public void NetworkGraph_ShouldNotUpdateGraphTwice()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex();

        // Act
        var vertices = _graph.Update(vertex);
        var final = _graph.Update(vertex);

        // Assert
        vertices.Count.Should().Be(2);
        final.Should().BeEmpty();
        _graph.Vertices.Count.Should().Be(2);
        var localVertex = _graph.Vertices.Single(item => item.Neighborhood.Address == _certificateManager.Address);
        localVertex.Neighborhood.Neighbors.Should().HaveCount(1);
        localVertex.Neighborhood.Neighbors.Should().Contain(vertex.Neighborhood.Address);
        var otherVertex = _graph.Vertices.Single(item => item.Neighborhood.Address == vertex.Neighborhood.Address);
        otherVertex.Neighborhood.Neighbors.Should().HaveCount(1);
        otherVertex.Neighborhood.Neighbors.Should().Contain(localVertex.Neighborhood.Address);
        _graph.Vertices.Should().Contain(item => item.Neighborhood.Address == _certificateManager.Address);
        _graph.Vertices.Should().Contain(item => item.Neighborhood.Address == vertex.Neighborhood.Address);
    }

    [Fact]
    public void NetworkGraph_ShouldAddNeighbor()
    {
        // Arrange

        // Act
        var (localVertex, updated) = _graph.AddAdjacency([PKey.Address1]);

        // Assert
        updated.Should().BeTrue();
        localVertex.Neighborhood.Address.Should().Be(_certificateManager.Address);
        localVertex.PublicKey.Should().Be(_certificateManager.PublicKey);
        localVertex.Neighborhood.Neighbors.Should().HaveCount(1).And.Contain(PKey.Address1);
    }

    [Fact]
    public void NetworkGraph_ShouldNotAddNeighborTwice()
    {
        // Arrange

        // Act
        var (localVertex1, updated1) = _graph.AddAdjacency([PKey.Address1]);
        var (localVertex2, updated2) = _graph.AddAdjacency([PKey.Address1]);

        // Assert
        updated1.Should().BeTrue();
        updated2.Should().BeFalse();
        localVertex1.Neighborhood.Neighbors.Should().Contain(PKey.Address1).And.HaveCount(1);
        localVertex2.Should().Be(localVertex1);
    }

    [Fact]
    public void NetworkGraph_ShouldNotAddAdjacencyWithInvalidAddress()
    {
        // Arrange

        // Act
        var (localVertex, updated) = _graph.AddAdjacency(["ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffxy"]);

        // Assert
        updated.Should().BeFalse();
    }

    [Fact]
    public void NetworkGraph_ShouldAddAndRemoveNeighbor()
    {
        // Arrange

        // Act
        var (localVertex1, updated1) = _graph.AddAdjacency([PKey.Address1]);
        var (localVertex2, updated2) = _graph.RemoveAdjacency([PKey.Address1]);

        // Assert
        updated1.Should().BeTrue();
        updated2.Should().BeTrue();
        localVertex1.Neighborhood.Neighbors.Should().Contain(PKey.Address1).And.HaveCount(1);
        localVertex2.Neighborhood.Neighbors.Should().BeEmpty();
    }
}
