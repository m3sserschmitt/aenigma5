using Autofac;
using Enigma5.App.Data;
using Enigma5.App.Security;
using Enigma5.Crypto.DataProviders;

namespace Enigma5.App.Tests.Data;

public class NetworkGraphTests : AppTestBase
{
    private readonly CertificateManager _certificateManager;

    private readonly NetworkGraph _graph;

    public NetworkGraphTests()
    {
        _certificateManager = _scope.Resolve<CertificateManager>();
        _graph = _scope.Resolve<NetworkGraph>();
    }

    [Fact]
    public void NetworkGraph_ShouldUpdateLocalVertex()
    {
        // Arrange
        var vertex = _scope.ResolveLocalVertex(new List<string> { PKey.Address2 });

        // Act
        var (vertices, delta) = _graph.Update(vertex);

        // Assert
        vertices.Count.Should().Be(1);
        var vertex1 = vertices.First();
        vertex1.PublicKey.Should().Be(_certificateManager.PublicKey);
        vertex1.Neighborhood.Address.Should().Be(_certificateManager.Address);
        vertex1.Neighborhood.Neighbors.Should().HaveCount(1);
        vertex1.Neighborhood.Neighbors.First().Should().Be(PKey.Address2);
        delta.Vertex.Should().BeNull();
        delta.Added.Should().BeFalse();
        _graph.Vertices.Should().HaveCount(1);
    }

    [Fact]
    public void NetworkGraph_ShouldAddNewVertex()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex(new List<string>() { PKey.Address2 });

        // Act
        var (vertices, delta) = _graph.Update(vertex);

        // Assert
        vertices.Count.Should().Be(2);
        delta.Vertex.Should().Be(vertex);
        delta.Added.Should().BeTrue();
        var localVertex = vertices.Single(item => item.Neighborhood.Address == _certificateManager.Address);
        localVertex.PublicKey.Should().Be(_certificateManager.PublicKey);
        localVertex.Neighborhood.Neighbors.Should().HaveCount(1);
        localVertex.Neighborhood.Neighbors.Single().Should().Be(vertex.Neighborhood.Address);
        var adjacentVertex = vertices.Single(item => item.Neighborhood.Address == PKey.Address1);
        adjacentVertex.Neighborhood.Neighbors.Should().HaveCount(2);
        adjacentVertex.Neighborhood.Neighbors.Should().Contain(PKey.Address2);
        adjacentVertex.Neighborhood.Neighbors.Should().Contain(_certificateManager.Address);
    }

    [Fact]
    public void NetworkGraph_ShouldRemoveAdjacency()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex(new List<string> { PKey.Address2 });

        var updatedVertex = _scope.ResolveNonAdjacentVertex(new List<string> { PKey.Address2 });

        // Act
        var (vertices, _) = _graph.Update(vertex);
        var (final, delta) = _graph.Update(updatedVertex);

        // Assert
        vertices.Count.Should().Be(2);
        final.Count.Should().Be(2);
        _graph.Vertices.Count.Should().Be(2);
        delta.Vertex.Should().Be(updatedVertex);
        delta.Added.Should().BeFalse();
        var localVertex = _graph.Vertices.Single(item => item.Neighborhood.Address == _certificateManager.Address);
        localVertex.Neighborhood.Neighbors.Should().BeEmpty();
        var otherVertex = _graph.Vertices.Single(item => item.Neighborhood.Address == vertex.Neighborhood.Address);
        otherVertex.Neighborhood.Neighbors.Should().HaveCount(1);
        otherVertex.Neighborhood.Neighbors.Should().Contain(PKey.Address2);
    }

    [Fact]
    public void NetworkGraph_ShouldNotUpdateGraph()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex(new List<string> { PKey.Address2 });

        // Act
        var (vertices, delta) = _graph.Update(vertex);
        var final = _graph.Update(vertex);

        // Assert
        vertices.Count.Should().Be(2);
        final.vertices.Should().BeEmpty();
        _graph.Vertices.Count.Should().Be(2);
        var localVertex = _graph.Vertices.Single(item => item.Neighborhood.Address == _certificateManager.Address);
        localVertex.Neighborhood.Neighbors.Should().HaveCount(1);
        localVertex.Neighborhood.Neighbors.Should().Contain(vertex.Neighborhood.Address);
        var otherVertex = _graph.Vertices.Single(item => item.Neighborhood.Address == vertex.Neighborhood.Address);
        otherVertex.Neighborhood.Neighbors.Should().HaveCount(2);
        otherVertex.Neighborhood.Neighbors.Should().Contain(PKey.Address2);
        otherVertex.Neighborhood.Neighbors.Should().Contain(localVertex.Neighborhood.Address);
    }

    [Fact]
    public void NetworkGraph_ShouldAddNeighbor()
    {
        // Arrange

        // Act
        var (localVertex, updated) = _graph.AddAdjacency(PKey.Address1);

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
        var (localVertex1, updated1) = _graph.AddAdjacency(PKey.Address1);
        var (localVertex2, updated2) = _graph.AddAdjacency(PKey.Address1);

        // Assert
        updated1.Should().BeTrue();
        updated2.Should().BeFalse();
        localVertex1.Neighborhood.Neighbors.Should().Contain(PKey.Address1).And.HaveCount(1);
        localVertex2.Should().Be(localVertex1);
    }

    [Fact]
    public void NetworkGraph_ShouldAddAndRemoveNeighbor()
    {
        // Arrange

        // Act
        var (localVertex1, updated1) = _graph.AddAdjacency(PKey.Address1);
        var (localVertex2, updated2) = _graph.RemoveAdjacency(PKey.Address1);

        // Assert
        updated1.Should().BeTrue();
        updated2.Should().BeTrue();
        localVertex1.Neighborhood.Neighbors.Should().Contain(PKey.Address1).And.HaveCount(1);
        localVertex2.Neighborhood.Neighbors.Should().BeEmpty();
    }
}
