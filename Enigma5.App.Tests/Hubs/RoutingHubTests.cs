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

using System.Diagnostics.CodeAnalysis;
using Enigma5.App.Data.Extensions;
using Enigma5.App.Hubs;
using Enigma5.App.Models;
using Enigma5.App.Models.HubInvocation;
using Enigma5.App.Tests.Helpers;
using Enigma5.Crypto.DataProviders;
using Enigma5.Crypto.Extensions;
using FluentAssertions;
using Xunit;

namespace Enigma5.App.Tests.Hubs;

[ExcludeFromCodeCoverage]
public class RoutingHubTests : AppTestBase
{
    [Fact]
    public async Task ShouldGenerateNonce()
    {
        // Arrange

        // Act
        var result = await _hub.GenerateToken();

        result.Should().NotBeNull();
        result.Should().BeOfType<SuccessResult<string>>();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.IsValidBase64().Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotGenerateTokenTwice()
    {
        // Arrange

        // Act
        var result1 = await _hub.GenerateToken();
        var result2 = await _hub.GenerateToken();

        result1.Should().NotBeNull();
        result1.Should().BeOfType<SuccessResult<string>>();
        result1.Success.Should().BeTrue();
        result1.Errors.Should().BeEmpty();
        result1.Data.IsValidBase64().Should().BeTrue();
        result2.Should().NotBeNull();
        result2.Should().BeOfType<ErrorResult<string>>();
        result2.Success.Should().BeFalse();
        result2.Data.Should().BeNull();
        result2.Errors.Count().Should().Be(1);
        result2.Errors.Single().Message.Should().Be(InvocationErrors.NONCE_GENERATION_ERROR);
    }

    [Fact]
    public async Task ShouldPullPendingMessages()
    {
        // Arrange
        var pendingMessage = await _dataSeeder.PendingMessage;
        _hub.ClientAddress = pendingMessage!.Destination;

        // Act
        var result = await _hub.Pull();

        result.Should().NotBeNull();
        result.Should().BeOfType<SuccessResult<List<PendingMessage>>>();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(1);
        var returnedMessage = result.Data.Single();
        returnedMessage.Destination.Should().Be(pendingMessage.Destination);
        returnedMessage.Content.Should().Be(pendingMessage.Content);
        returnedMessage.DateReceived.Should().Be(pendingMessage.DateReceived);
    }

    [Fact]
    public async Task ShouldNotPullPendingMessagesTwice()
    {
        // Arrange
        var pendingMessage = await _dataSeeder.PendingMessage;
        _hub.ClientAddress = pendingMessage!.Destination;

        // Act
        var result1 = await _hub.Pull();
        var result2 = await _hub.Pull();

        result1.Should().NotBeNull();
        result1.Success.Should().BeTrue();
        result1.Errors.Should().BeEmpty();
        result1.Data.Should().NotBeNull();
        result1.Data!.Count.Should().Be(1);
        result2.Success.Should().BeTrue();
        result2.Data.Should().NotBeNull();
        result2.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldAuthenticate()
    {
        // Arrange
        var nonce = _sessionManager.AddPending(_hub.Context.ConnectionId);
        var request = DataSeeder.CreateAuthenticationRequest(nonce!);

        // Act
        var result = await _hub.Authenticate(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SuccessResult<bool>>();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotAuthenticateTwice()
    {
        // Arrange
        var nonce = _sessionManager.AddPending(_hub.Context.ConnectionId);
        var request = DataSeeder.CreateAuthenticationRequest(nonce!);

        // Act
        var result1 = await _hub.Authenticate(request);
        var result2 = await _hub.Authenticate(request);

        // Assert
        result1.Success.Should().BeTrue();
        result1.Errors.Should().BeEmpty();
        result1.Data.Should().BeTrue();
        result2.Success.Should().BeFalse();
        result2.Errors.Should().HaveCount(1);
        result2.Errors.Single().Message.Should().Be(InvocationErrors.INVALID_NONCE_SIGNATURE);
        result2.Data.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldSignNonce()
    {
        // Arrange
        var request = DataSeeder.CreateSignatureRequest();

        // Act
        var result = await _hub.SignToken(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SuccessResult<Signature>>();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().NotBeNull();
        result.Data!.PublicKey.Should().Be(_certificateManager.PublicKey);
        result.Data.SignedData.IsValidBase64().Should().BeTrue();
        Convert.FromBase64String(result.Data.SignedData).GetDataFromSignature(_certificateManager.PublicKey).Should().Equal(Convert.FromBase64String(request.Nonce!));
    }

    [Fact]
    public async Task ShouldBroadcast()
    {
        // Arrange
        var vertex = _container.ResolveAdjacentVertex();
        var request = vertex.ToVertexBroadcast();

        // Act
        var result = await _hub.Broadcast(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SuccessResult<bool>>();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().BeTrue();
        _graph.Vertices.Count.Should().Be(2);
        _graph.Vertices.Should().Contain(vertex);
        _graph.LocalVertex.Should().NotBeNull();
        _graph.LocalVertex!.Neighborhood.Neighbors.Should().HaveCount(1);
        _graph.LocalVertex!.Neighborhood.Neighbors.Should().Contain(vertex.Neighborhood.Address);
    }

    [Fact]
    public async Task ShouldNotBroadcastWithInvalidKey()
    {
        // Arrange
        var vertex = _container.ResolveAdjacentVertex();
        var request = new VertexBroadcastRequest {
            PublicKey = "invalid-key",
            SignedData = vertex.SignedData
        };

        // Act
        var result = await _hub.Broadcast(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ErrorResult<bool>>();
        result.Success.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.Single().Message.Should().Be(InvocationErrors.BROADCAST_HANDLING_ERROR);
        _graph.Vertices.Count.Should().Be(1);
        _graph.Vertices.Should().NotContain(vertex);
    }

    [Fact]
    public async Task ShouldNotBroadcastWithInvalidSignedData()
    {
        // Arrange
        var request = new VertexBroadcastRequest {
            PublicKey = PKey.PublicKey1,
            SignedData = "invalid-signed-data"
        };

        // Act
        var result = await _hub.Broadcast(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ErrorResult<bool>>();
        result.Success.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.Single().Message.Should().Be(InvocationErrors.BROADCAST_HANDLING_ERROR);
        _graph.Vertices.Count.Should().Be(1);
    }

    [Fact]
    public async Task ShouldTriggerBroadcast()
    {
        // Arrange
        var request = new TriggerBroadcastRequest {
            NewAddresses = [ PKey.Address1 ]
        };

        // Act
        var result = await _hub.TriggerBroadcast(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SuccessResult<bool>>();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().BeTrue();
        _graph.Vertices.Count.Should().Be(1);
        _graph.LocalVertex.Should().NotBeNull();
        _graph.LocalVertex!.Neighborhood.Neighbors.Should().HaveCount(1);
        _graph.LocalVertex!.Neighborhood.Neighbors.Should().Contain(PKey.Address1);
    }

    [Fact]
    public async Task ShouldTriggerBroadcastWithoutNewAddress()
    {
        // Arrange
        var request = new TriggerBroadcastRequest();

        // Act
        var result = await _hub.TriggerBroadcast(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SuccessResult<bool>>();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().BeTrue();
        _graph.Vertices.Count.Should().Be(1);
        _graph.LocalVertex.Should().NotBeNull();
        _graph.LocalVertex!.Neighborhood.Neighbors.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldRouteMessage()
    {
        // Arrange
        _hub.Content = [0x01, 0x02, 0x03];
        _hub.DestinationConnectionId = "test-destination-connection-id";

        // Act
        var result = await _hub.RouteMessage(new());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SuccessResult<bool>>();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldStorePendingMessage()
    {
        // Arrange
        _hub.Content = [0x01, 0x02, 0x03];
        _hub.Next = PKey.Address2;

        // Act
        var result = await _hub.RouteMessage(new());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SuccessResult<bool>>();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().BeTrue();
        _dbContext.Messages.FirstOrDefault(item => item.Destination == PKey.Address2 && item.Content == "AQID").Should().NotBeNull();
    }
}
