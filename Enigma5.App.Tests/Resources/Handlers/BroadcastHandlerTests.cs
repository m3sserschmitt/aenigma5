/*
    Aenigma - Onion Routing based messaging application
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
using Enigma5.App.Data.Extensions;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Handlers;

namespace Enigma5.App.Tests.Resources.Handlers;

public class BroadcastHandlerTests : AppTestBase
{
    private readonly BroadcastHandler _handler;

    public BroadcastHandlerTests()
    {
        _handler = _scope.Resolve<BroadcastHandler>();
    }

    [Fact]
    public async void ShouldAddNewNeighbor()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex();
        var broadcast = vertex.ToVertexBroadcast();
        var request = new HandleBroadcastCommand(broadcast);

        // Act
        var (localVertex, broadcasts) = await _handler.Handle(request);

        // Assert
        localVertex.Should().BeOfType<Vertex>();
        broadcasts.Should().AllBeOfType<VertexBroadcastRequest>();
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
        var broadcast = vertex.ToVertexBroadcast();
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
        var initialBroadcast = adjacentVertex.ToVertexBroadcast();
        var finalBroadcast = nonAdjacentVertex.ToVertexBroadcast();
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
