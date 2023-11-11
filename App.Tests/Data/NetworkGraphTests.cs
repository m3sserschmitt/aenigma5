using App.Tests;
using Autofac;
using Enigma5.App.Data;
using Enigma5.App.Security;
using Enigma5.Crypto.DataProviders;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Enigma5.App.Tests;

public class NetworkGraphTests
{
    private readonly IContainer _container;

    private readonly CertificateManager _certificateManager;

    public NetworkGraphTests()
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<NetworkGraph>();
        builder.RegisterType<CertificateManager>().SingleInstance();
        builder.RegisterVertex();
        builder.Register(_ => Substitute.For<IConfiguration>());

        _container = builder.Build();

        _certificateManager = _container.Resolve<CertificateManager>();
    }

    [Fact]
    public void NetworkGraph_ShouldUpdateLocalVertex()
    {
        // Arrange
        using var scope = _container.BeginLifetimeScope();
        var graph = scope.Resolve<NetworkGraph>();
        var vertex = scope.ResolveLocalVertex(new() { PKey.Address2 });

        // Act
        var (vertices, delta) = graph.Add(vertex);

        // Assert
        vertices.Count.Should().Be(1);
        var vertex1 = vertices.First();
        vertex1.PublicKey.Should().Be(_certificateManager.PublicKey);
        vertex1.Neighborhood.Address.Should().Be(_certificateManager.Address);
        vertex1.Neighborhood.Neighbors.Should().HaveCount(1);
        vertex1.Neighborhood.Neighbors.First().Should().Be(PKey.Address2);
        delta.Vertex.Should().BeNull();
        delta.Added.Should().BeFalse();
        graph.Vertices.Should().HaveCount(1);
    }

    [Fact]
    public void NetworkGraph_ShouldAddNewVertex()
    {
        // Arrange
        using var scope = _container.BeginLifetimeScope();
        var graph = scope.Resolve<NetworkGraph>();
        var vertex = scope.ResolveAdjacentVertex(new List<string>() { PKey.Address2 });

        // Act
        var (vertices, delta) = graph.Add(vertex);

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
        using var scope = _container.BeginLifetimeScope();
        var graph = scope.Resolve<NetworkGraph>();
        var vertex = scope.ResolveAdjacentVertex(new() { PKey.Address2 });

        var updatedVertex = scope.ResolveNonAdjacentVertex(new() { PKey.Address2 });

        // Act
        var (vertices, _) = graph.Add(vertex);
        var (final, delta) = graph.Add(updatedVertex);

        // Assert
        vertices.Count.Should().Be(2);
        final.Count.Should().Be(2);
        graph.Vertices.Count.Should().Be(2);
        delta.Vertex.Should().Be(updatedVertex);
        delta.Added.Should().BeFalse();
        var localVertex = graph.Vertices.Single(item => item.Neighborhood.Address == _certificateManager.Address);
        localVertex.Neighborhood.Neighbors.Should().BeEmpty();
        var otherVertex = graph.Vertices.Single(item => item.Neighborhood.Address == vertex.Neighborhood.Address);
        otherVertex.Neighborhood.Neighbors.Should().HaveCount(1);
        otherVertex.Neighborhood.Neighbors.Should().Contain(PKey.Address2);
    }

    [Fact]
    public void NetworkGraph_ShouldNotUpdateGraph()
    {
        // Arrange
        using var scope = _container.BeginLifetimeScope();
        var graph = scope.Resolve<NetworkGraph>();
        var vertex = scope.ResolveAdjacentVertex(new() { PKey.Address2 });

        // Act
        var (vertices, delta) = graph.Add(vertex);
        var final = graph.Add(vertex);

        // Assert
        vertices.Count.Should().Be(2);
        final.vertices.Should().BeEmpty();
        graph.Vertices.Count.Should().Be(2);
        var localVertex = graph.Vertices.Single(item => item.Neighborhood.Address == _certificateManager.Address);
        localVertex.Neighborhood.Neighbors.Should().HaveCount(1);
        localVertex.Neighborhood.Neighbors.Should().Contain(vertex.Neighborhood.Address);
        var otherVertex = graph.Vertices.Single(item => item.Neighborhood.Address == vertex.Neighborhood.Address);
        otherVertex.Neighborhood.Neighbors.Should().HaveCount(2);
        otherVertex.Neighborhood.Neighbors.Should().Contain(PKey.Address2);
        otherVertex.Neighborhood.Neighbors.Should().Contain(localVertex.Neighborhood.Address);
    }
}
