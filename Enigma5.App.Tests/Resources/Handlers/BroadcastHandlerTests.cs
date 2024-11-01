﻿/*
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
using Enigma5.App.Data.Extensions;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Handlers;

namespace Enigma5.App.Tests.Resources.Handlers;

public class BroadcastHandlerTests : AppTestBase
{
    private readonly BroadcastHandler _handler;

    private readonly NetworkGraph _graph;

    public BroadcastHandlerTests()
    {
        _handler = _scope.Resolve<BroadcastHandler>();
        _graph = _scope.Resolve<NetworkGraph>();
    }

    [Fact]
    public async void ShouldAddNewNeighbor()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex();
        var broadcast = vertex.ToVertexBroadcast();
        var request = new HandleBroadcastCommand(broadcast);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        var localVertex = _graph.LocalVertex;
        var broadcasts = result.Value;
        broadcast.Should().NotBeNull();
        localVertex.Should().BeOfType<Vertex>();
        broadcasts.Should().AllBeOfType<VertexBroadcastRequest>();
        localVertex!.Neighborhood.Neighbors.Single().Should().Be(vertex.Neighborhood.Address);
        broadcasts.Should().HaveCount(2);
        var broadcastLocal = broadcasts!.Single(item => item.PublicKey == localVertex.PublicKey);
        var broadcastRemote = broadcasts!.Single(item => item.PublicKey == vertex.PublicKey);
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
        var result1 = await _handler.Handle(request);
        var localVertex1 = _graph.LocalVertex;
        var result2 = await _handler.Handle(request);
        var localVertex2 = _graph.LocalVertex;

        // Assert
        var broadcasts1 = result1.Value;
        var broadcasts2 = result2.Value;
        broadcasts1.Should().NotBeNull();
        broadcasts2.Should().NotBeNull();
        localVertex1!.Neighborhood.Neighbors.Single().Should().Be(vertex.Neighborhood.Address);
        broadcasts1.Should().HaveCount(2);
        localVertex2!.Neighborhood.Neighbors.Single().Should().Be(vertex.Neighborhood.Address);
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
        var result1 = await _handler.Handle(request1);
        var localVertex1 = _graph.LocalVertex;
        var result2 = await _handler.Handle(request2);
        var localVertex2 = _graph.LocalVertex;

        // Assert
        var broadcasts1 = result1.Value;
        var broadcasts2 = result2.Value;
        broadcasts1.Should().NotBeNull();
        broadcasts2.Should().NotBeNull();
        localVertex1!.Neighborhood.Neighbors.Single().Should().Be(adjacentVertex.Neighborhood.Address);
        broadcasts1.Should().HaveCount(2);
        localVertex2!.Neighborhood.Neighbors.Should().BeEmpty();
        broadcasts2.Should().HaveCount(2);
        broadcasts2.Should().Contain(item => item.PublicKey == adjacentVertex.PublicKey);
        broadcasts2.Should().Contain(item => item.PublicKey == localVertex1.PublicKey);
        broadcasts1.Should().Contain(item => item.PublicKey == adjacentVertex.PublicKey);
        broadcasts1.Should().Contain(item => item.PublicKey == localVertex1.PublicKey);
    }
}
