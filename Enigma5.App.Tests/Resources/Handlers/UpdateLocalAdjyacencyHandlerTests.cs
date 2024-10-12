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
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Handlers;
using Enigma5.Security.Contracts;
using Enigma5.Crypto.DataProviders;

namespace Enigma5.App.Tests.Resources.Handlers;

public class UpdateLocalAdjacencyHandlerTests : AppTestBase
{
    private readonly ICertificateManager _certificateManager;

    private readonly UpdateLocalAdjacencyHandler _handler;

    public UpdateLocalAdjacencyHandlerTests()
    {
        _certificateManager = _scope.Resolve<ICertificateManager>();
        _handler = _scope.Resolve<UpdateLocalAdjacencyHandler>();
    }

    [Fact]
    public async void ShouldAddNewNeighbor()
    {
        // Arrange
        var request = new UpdateLocalAdjacencyCommand([PKey.Address1], true);

        // Act
        var (localVertex, broadcast) = await _handler.Handle(request);

        localVertex.Should().BeOfType<Vertex>();
        localVertex.PublicKey.Should().Be(_certificateManager.PublicKey);
        localVertex.SignedData.Should().NotBeEmpty();
        localVertex.Neighborhood.Address.Should().Be(_certificateManager.Address);
        localVertex.Neighborhood.Neighbors.Should().HaveCount(1).And.Contain(PKey.Address1);
        broadcast.Should().NotBeNull().And.BeOfType<VertexBroadcastRequest>();
        broadcast!.SignedData.Should().NotBeEmpty();
        broadcast.PublicKey.Should().Be(_certificateManager.PublicKey);
        broadcast.SignedData.Should().Be(localVertex.SignedData);
    }

    [Fact]
    public async void ShouldNotAddNeighborTwice()
    {
        // Arrange
        var request = new UpdateLocalAdjacencyCommand([PKey.Address1], true);

        // Act
        var (localVertex1, broadcast1) = await _handler.Handle(request);
        var (localVertex2, broadcast2) = await _handler.Handle(request);

        // Assert
        localVertex1.Should().Be(localVertex2);
        localVertex1.Neighborhood.Neighbors.Should().HaveCount(1).And.Contain(PKey.Address1);
        broadcast1.Should().NotBeNull();
        broadcast2.Should().BeNull();
    }

    [Fact]
    public async void ShouldNotRemoveNonExistentNeighbor()
    {
        // Arrange
        var request = new UpdateLocalAdjacencyCommand([PKey.Address1], false);

        // Act
        var (localVertex, broadcast) = await _handler.Handle(request);

        // Assert
        localVertex.PublicKey.Should().Be(_certificateManager.PublicKey);
        localVertex.SignedData.Should().NotBeEmpty();
        localVertex.Neighborhood.Address.Should().Be(_certificateManager.Address);
        localVertex.Neighborhood.Neighbors.Should().BeEmpty();
        broadcast.Should().BeNull();
    }

    [Fact]
    public async void ShouldAddAndRemoveNeighbor()
    {
        // Arrange

        // Act
        var (localVertex1, broadcast1) = await _handler.Handle(new UpdateLocalAdjacencyCommand([PKey.Address1], true));
        var (localVertex2, broadcast2) = await _handler.Handle(new UpdateLocalAdjacencyCommand([PKey.Address1], false));

        // Assert
        localVertex1.Neighborhood.Neighbors.Should().HaveCount(1).And.Contain(PKey.Address1);
        broadcast1.Should().NotBeNull();
        localVertex2.Neighborhood.Neighbors.Should().BeEmpty();
        broadcast2.Should().NotBeNull();
    }
}
