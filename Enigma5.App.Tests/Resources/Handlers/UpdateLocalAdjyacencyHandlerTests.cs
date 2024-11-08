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
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Handlers;
using Enigma5.Crypto.DataProviders;
using Enigma5.App.Tests.Helpers;
using Xunit;
using FluentAssertions;
using System.Diagnostics.CodeAnalysis;

namespace Enigma5.App.Tests.Resources.Handlers;

[ExcludeFromCodeCoverage]
public class UpdateLocalAdjacencyHandlerTests : AppTestBase
{
    private readonly UpdateLocalAdjacencyHandler _handler;

    public UpdateLocalAdjacencyHandlerTests()
    {
        _handler = _container.Resolve<UpdateLocalAdjacencyHandler>();
    }

    [Fact]
    public async Task ShouldAddNewNeighbor()
    {
        // Arrange
        var request = new UpdateLocalAdjacencyCommand([PKey.Address1], true);

        // Act
        var result = await _handler.Handle(request);

        var localVertex = _graph.LocalVertex;
        var broadcast = result.Value;
        localVertex.Should().BeOfType<Enigma5.App.Data.Vertex>();
        localVertex!.PublicKey.Should().Be(_certificateManager.PublicKey);
        localVertex.SignedData.Should().NotBeEmpty();
        localVertex.Neighborhood.Address.Should().Be(_certificateManager.Address);
        localVertex.Neighborhood.Neighbors.Should().HaveCount(1).And.Contain(PKey.Address1);
        broadcast.Should().NotBeNull().And.BeOfType<VertexBroadcastRequest>();
        broadcast!.SignedData.Should().NotBeEmpty();
        broadcast.PublicKey.Should().Be(_certificateManager.PublicKey);
        broadcast.SignedData.Should().Be(localVertex.SignedData);
    }

    [Fact]
    public async Task ShouldNotAddNeighborTwice()
    {
        // Arrange
        var request = new UpdateLocalAdjacencyCommand([PKey.Address1], true);

        // Act
        var result1 = await _handler.Handle(request);
        var localVertex1 = _graph.LocalVertex;
        var result2 = await _handler.Handle(request);
        var localVertex2 = _graph.LocalVertex;

        // Assert
        localVertex1.Should().Be(localVertex2);
        localVertex1!.Neighborhood.Neighbors.Should().HaveCount(1).And.Contain(PKey.Address1);
        result1.Value.Should().NotBeNull();
        result2.Value.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotRemoveNonExistentNeighbor()
    {
        // Arrange
        var request = new UpdateLocalAdjacencyCommand([PKey.Address1], false);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        var localVertex = _graph.LocalVertex;
        localVertex!.PublicKey.Should().Be(_certificateManager.PublicKey);
        localVertex.SignedData.Should().NotBeEmpty();
        localVertex.Neighborhood.Address.Should().Be(_certificateManager.Address);
        localVertex.Neighborhood.Neighbors.Should().BeEmpty();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task ShouldAddAndRemoveNeighbor()
    {
        // Arrange

        // Act
        var result1 = await _handler.Handle(new UpdateLocalAdjacencyCommand([PKey.Address1], true));
        var localVertex1 = _graph.LocalVertex;
        var result2 = await _handler.Handle(new UpdateLocalAdjacencyCommand([PKey.Address1], false));
        var localVertex2 = _graph.LocalVertex;

        // Assert
        localVertex1!.Neighborhood.Neighbors.Should().HaveCount(1).And.Contain(PKey.Address1);
        result1.Value.Should().NotBeNull();
        localVertex2!.Neighborhood.Neighbors.Should().BeEmpty();
        result2.Should().NotBeNull();
    }
}
