using Autofac;
using AutoMapper;
using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Handlers;

namespace Enigma5.App.Tests.Resources.Handlers;

public class BroadcastHandlerTests : AppTestBase
{
    private readonly BroadcastHandler _handler;

    private readonly IMapper _mapper;

    public BroadcastHandlerTests()
    {
        _handler = _scope.Resolve<BroadcastHandler>();
        _mapper = _scope.Resolve<IMapper>();
    }

    [Fact]
    public async void ShouldAddNewNeighbor()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex();
        var broadcast = _mapper.Map<BroadcastAdjacencyList>(vertex);
        var request = new HandleBroadcastCommand(broadcast);

        // Act
        var (localVertex, broadcasts) = await _handler.Handle(request);

        // Assert
        localVertex.Should().BeOfType<Vertex>();
        broadcasts.Should().AllBeOfType<BroadcastAdjacencyList>();
        localVertex.Neighborhood.Neighbors.Single().Should().Be(vertex.Neighborhood.Address);
        broadcasts.Should().HaveCount(2);
        var broadcastLocal = broadcasts.Single(item => item.PublicKey == localVertex.PublicKey);
        var broadcastRemote = broadcasts.Single(item => item.PublicKey == vertex.PublicKey);
        broadcastLocal.SignedData.Should().Be(localVertex.SignedData);
        broadcastRemote.SignedData.Should().Be(vertex.SignedData);
    }

    [Fact]
    public async void ShouldNotAddNeighborTwice()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex();
        var broadcast = _mapper.Map<BroadcastAdjacencyList>(vertex);
        var request = new HandleBroadcastCommand(broadcast);

        // Act
        var (localVertex1, broadcasts1) = await _handler.Handle(request);
        var (localVertex2, broadcasts2) = await _handler.Handle(request);

        // Assert
        localVertex1.Neighborhood.Neighbors.Single().Should().Be(vertex.Neighborhood.Address);
        broadcasts1.Should().HaveCount(2);
        localVertex2.Neighborhood.Neighbors.Single().Should().Be(vertex.Neighborhood.Address);
        broadcasts2.Should().BeEmpty();
    }

    [Fact]
    public async void ShouldAddAndRemoveNeighbor()
    {
        // Arrange
        var adjacentVertex = _scope.ResolveAdjacentVertex();
        var nonAdjacentVertex = _scope.ResolveNonAdjacentVertex();
        var initialBroadcast = _mapper.Map<BroadcastAdjacencyList>(adjacentVertex);
        var finalBroadcast = _mapper.Map<BroadcastAdjacencyList>(nonAdjacentVertex);
        var request1 = new HandleBroadcastCommand(initialBroadcast);
        var request2 = new HandleBroadcastCommand(finalBroadcast);
    
        // Act
        var (localVertex1, broadcasts1) = await _handler.Handle(request1);
        var (localVertex2, broadcasts2) = await _handler.Handle(request2);

        // Assert
        localVertex1.Neighborhood.Neighbors.Single().Should().Be(adjacentVertex.Neighborhood.Address);
        broadcasts1.Should().HaveCount(2);
        localVertex2.Neighborhood.Neighbors.Should().BeEmpty();
        broadcasts2.Should().HaveCount(2);
        broadcasts2.Should().Contain(item => item.PublicKey == adjacentVertex.PublicKey);
        broadcasts2.Should().Contain(item => item.PublicKey == localVertex1.PublicKey);
        broadcasts1.Should().Contain(item => item.PublicKey == adjacentVertex.PublicKey);
        broadcasts1.Should().Contain(item => item.PublicKey == localVertex1.PublicKey);
    }
}
