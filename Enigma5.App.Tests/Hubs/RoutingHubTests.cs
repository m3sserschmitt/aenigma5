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
using Autofac;
using Enigma5.App.Data.Extensions;
using Enigma5.App.Hubs;
using Enigma5.App.Models;
using Enigma5.App.Models.HubInvocation;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Handlers;
using Enigma5.App.Resources.Queries;
using Enigma5.Crypto.Contracts;
using Enigma5.Crypto.DataProviders;
using Enigma5.Crypto.Extensions;
using Enigma5.Tests.Base;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace Enigma5.App.Tests.Hubs;

[ExcludeFromCodeCoverage]
public class RoutingHubTests : AppTestBase
{
    #region GENERATE_NONCE

    [Fact]
    public async Task ShouldGenerateNonce()
    {
        // Arrange

        // Act
        var result = await _hub.GenerateToken();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SuccessResult<string>>();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().Be(_testNonce1);
        _sessionManager.Received(1).AddPending(_testConnectionId1);
    }

    [Fact]
    public async Task ShouldReturnErrorWhenNonceNotGenerated()
    {
        // Arrange
        _sessionManager.AddPending("").ReturnsNullForAnyArgs();

        // Act
        var result = await _hub.GenerateToken();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ErrorResult<string>>();
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.Single().Message.Should().Be(InvocationErrors.NONCE_GENERATION_ERROR);
    }    

    #endregion GENERATE_NONCE

    #region PULL

    [Fact]
    public async Task ShouldPullPendingMessages()
    {
        // Arrange
        var pendingMessage = DataSeeder.DataFactory.PendingMesage;
        var oldPendingMesage = DataSeeder.DataFactory.OldPendingMesage;
        var deliveredPendingMessage = DataSeeder.DataFactory.DeliveredPendingMesage;
        _hub.ClientAddress = pendingMessage!.Destination;

        // Act
        var result = await _hub.Pull();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SuccessResult<List<PendingMessage>>>();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(3);
        result.Data.FirstOrDefault(item =>
        item.Destination == pendingMessage.Destination
        && item.Content == pendingMessage.Content
        && item.DateReceived == pendingMessage.DateReceived
        && !item.Sent
        && item.Uuid == pendingMessage.Uuid).Should().NotBeNull();
        result.Data.FirstOrDefault(item =>
        item.Destination == oldPendingMesage.Destination
        && item.Content == oldPendingMesage.Content
        && item.DateReceived == oldPendingMesage.DateReceived
        && !item.Sent
        && item.Uuid == oldPendingMesage.Uuid).Should().NotBeNull();
        result.Data.FirstOrDefault(item =>
        item.Destination == deliveredPendingMessage.Destination
        && item.Content == deliveredPendingMessage.Content
        && item.DateReceived == deliveredPendingMessage.DateReceived
        && item.Sent
        && item.Uuid == deliveredPendingMessage.Uuid).Should().NotBeNull();
        var pendingMessages = await _dbContext.Messages.Where(item => item.Destination == pendingMessage.Destination).ToListAsync();
        pendingMessages.Should().HaveCount(3);
        pendingMessages.Should().OnlyContain(item => item.Sent && item.DateSent != null);
    }

    [Fact]
    public async Task ShouldNotPullPendingMessagesWhenClientAddressNull()
    {
        // Arrange
        _hub.ClientAddress = null;

        // Act
        var result = await _hub.Pull();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ErrorResult<List<PendingMessage>>>();
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Data.Should().BeNull();
        result.Errors.Single().Message.Should().Be(InvocationErrors.INTERNAL_ERROR);
    }

    [Fact]
    public async Task ShouldNotPullWhenGetPendingMessagesQueryFails()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<RoutingHub>>();
        mediator.Send(Arg.Any<GetPendingMessagesByDestinationQuery>()).ReturnsForAnyArgs(Task.FromResult(CommandResult.CreateResultFailure<List<PendingMessage>>()));
        var hub = new RoutingHub(_sessionManager, _certificateManager, _graph, mediator, logger)
        {
            ClientAddress = PKey.Address2
        };
        ConfigureSignalRHub(hub);

        // Act
        var result = await hub.Pull();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.Single().Message.Should().Be(InvocationErrors.INTERNAL_ERROR);
        result.Data.Should().BeNull();
        logger.ReceivedWithAnyArgs(1).LogError("");
    }

    #endregion PULL

    #region CLEANUP

    [Fact]
    public async Task ShouldCleanup()
    {
        // Arrange
        var pendingMessage = DataSeeder.DataFactory.PendingMesage;
        _hub.ClientAddress = pendingMessage!.Destination;

        // Act
        var result = await _hub.Cleanup();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().BeTrue();
        var pendingMessages = await _dbContext.Messages.Where(item => item.Destination == pendingMessage.Destination).ToListAsync();
        pendingMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldNotCleanupWhenClientAddressNull()
    {
        // Arrange
        var pendingMessage = DataSeeder.DataFactory.PendingMesage;
        _hub.ClientAddress = null;

        // Act
        var result = await _hub.Cleanup();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.Single().Message.Should().Be(InvocationErrors.INTERNAL_ERROR);
        result.Data.Should().BeFalse();
        var pendingMessages = await _dbContext.Messages.Where(item => item.Destination == pendingMessage.Destination).ToListAsync();
        pendingMessages.Should().HaveCount(3);
    }

    [Fact]
    public async Task ShouldReturnErrorWhenRemoveMessagesCommandFails()
    {
        // Arrange
        var pendingMessage = DataSeeder.DataFactory.PendingMesage;
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<RoutingHub>>();
        mediator.Send(Arg.Any<RemoveMessagesCommand>()).ReturnsForAnyArgs(Task.FromResult(CommandResult.CreateResultFailure<int>()));
        var hub = new RoutingHub(_sessionManager, _certificateManager, _graph, mediator, logger)
        {
            ClientAddress = PKey.Address2
        };
        ConfigureSignalRHub(hub);

        // Act
        var result = await hub.Cleanup();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.Single().Message.Should().Be(InvocationErrors.INTERNAL_ERROR);
        result.Data.Should().BeFalse();
        logger.ReceivedWithAnyArgs(1).LogError("");
        var pendingMessages = await _dbContext.Messages.Where(item => item.Destination == pendingMessage.Destination).ToListAsync();
        pendingMessages.Should().HaveCount(3);
    }

    #endregion CLEANUP

    #region AUTHENTICATE

    [Fact]
    public async Task ShouldAuthenticate()
    {
        // Arrange
        var request = new AuthenticationRequest {
            PublicKey = PKey.PublicKey1,
            Signature = "test-signature"
        };

        // Act
        var result = await _hub.Authenticate(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SuccessResult<bool>>();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().BeTrue();
        _sessionManager.Received(1).Authenticate(_testConnectionId1, request.PublicKey, request.Signature);
    }

    [Fact]
    public async Task ShouldReturnErrorWhenAuthenticationFails()
    {
        // Arrange
        var request = new AuthenticationRequest {
            PublicKey = PKey.PublicKey1,
            Signature = "test-signature"
        };
        _sessionManager.Authenticate("", "", "").ReturnsForAnyArgs(false);

        // Act
        var result = await _hub.Authenticate(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ErrorResult<bool>>();
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.Single().Message.Should().Be(InvocationErrors.INVALID_NONCE_SIGNATURE);
    }

    #endregion AUTHENTICATE

    #region SIGN_NONCE

    [Fact]
    public async Task ShouldSignNonce()
    {
        // Arrange
        var request = DataSeeder.ModelsFactory.CreateSignatureRequest();

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

    #endregion SIGN_NONCE

    #region BROADCAST

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
        _graph.LocalVertex.Should().NotBeNull();
        _graph.Vertices.Count.Should().Be(2);
        _graph.Vertices.TryGetValue(_graph.LocalVertex!, out Enigma5.App.Data.Vertex? _).Should().BeTrue();
        _graph.Vertices.TryGetValue(vertex, out Enigma5.App.Data.Vertex? _).Should().BeTrue();
        _graph.LocalVertex!.Neighborhood.Neighbors.Should().HaveCount(1);
        _graph.LocalVertex!.Neighborhood.Neighbors.Should().Contain(vertex.Neighborhood.Address);
        _hub.Clients.Received(2).Client(_testConnectionId1);
        await _hub.Clients.Client(_testConnectionId1).ReceivedWithAnyArgs(2).SendAsync("");
    }

    [Fact]
    public async Task ShouldNotBroadcastWithInvalidKey()
    {
        // Arrange
        var vertex = _container.ResolveAdjacentVertex();
        var request = new VertexBroadcastRequest("invalid-key", vertex.SignedData!);

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
        _graph.Vertices.TryGetValue(vertex, out Enigma5.App.Data.Vertex? _).Should().BeFalse();
        _hub.Clients.DidNotReceiveWithAnyArgs().Client("");
    }

    [Fact]
    public async Task ShouldNotBroadcastWithInvalidSignedData()
    {
        // Arrange
        var request = new VertexBroadcastRequest(PKey.PublicKey1, "invalid-signed-data");

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
        _hub.Clients.DidNotReceiveWithAnyArgs().Client("");
    }

    [Fact]
    public async Task ShouldReturnErrorOnBroadcastWhenSendAsyncFails()
    {
        // Arrange
        var singleClientProxy = Substitute.For<ISingleClientProxy>();
        singleClientProxy.SendAsync("", default).ThrowsForAnyArgs(new Exception("client not reachable"));
        _hub.Clients.Client(_testConnectionId1).Returns(singleClientProxy);
        var vertex = _container.ResolveAdjacentVertex();
        var request = vertex.ToVertexBroadcast();

        // Act
        var result = await _hub.Broadcast(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ErrorResult<bool>>();
        result.Success.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.Single().Message.Should().Be(InvocationErrors.BROADCAST_FORWARDING_ERROR);
    }

    #endregion BROADCAST

    #region TRIGGER_BROADCAST
        
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
        _graph.LocalVertex.Should().NotBeNull();
        _graph.Vertices.Count.Should().Be(1);
        _graph.Vertices.TryGetValue(_graph.LocalVertex!, out Enigma5.App.Data.Vertex? _).Should().BeTrue();
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
        _graph.LocalVertex.Should().NotBeNull();
        _graph.Vertices.Count.Should().Be(1);
        _graph.Vertices.TryGetValue(_graph.LocalVertex!, out Enigma5.App.Data.Vertex? _).Should().BeTrue();
        _graph.LocalVertex.Should().NotBeNull();
        _graph.LocalVertex!.Neighborhood.Neighbors.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldNotTriggerBroadcastWhenAddAdjacencyCommandFails()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateLocalAdjacencyCommand>()).ReturnsForAnyArgs(Task.FromResult(CommandResult.CreateResultFailure<VertexBroadcastRequest>()));
        var logger = Substitute.For<ILogger<RoutingHub>>();
        var hub = new RoutingHub(_sessionManager, _certificateManager, _graph, mediator, logger);
        ConfigureSignalRHub(hub);
        var request = new TriggerBroadcastRequest {
            NewAddresses = [ PKey.Address1 ]
        };

        // Act
        var result = await hub.TriggerBroadcast(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ErrorResult<bool>>();
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.Single().Message.Should().Be(InvocationErrors.BROADCAST_TRIGGERING_WARNING);
        result.Data.Should().BeTrue();
        _graph.LocalVertex.Should().NotBeNull();
        _graph.Vertices.Count.Should().Be(1);
        _graph.Vertices.TryGetValue(_graph.LocalVertex!, out Enigma5.App.Data.Vertex? _).Should().BeTrue();
        _graph.LocalVertex!.Neighborhood.Neighbors.Should().BeEmpty();
        hub.Clients.DidNotReceiveWithAnyArgs().Client("");
        logger.ReceivedWithAnyArgs(1).LogWarning("");
    }

    #endregion TRIGGER_BROADCAST

    #region ROUTE_MESSAGE

    [Fact]
    public async Task ShouldRouteMessage()
    {
        // Arrange
        _hub.Content = [0x01, 0x02, 0x03];
        _hub.DestinationConnectionId = _testConnectionId2;

        // Act
        var result = await _hub.RouteMessage(new());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SuccessResult<bool>>();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().BeTrue();
        _hub.Clients.Received(1).Client(_testConnectionId2);
        await _hub.Clients.Client(_testConnectionId2).ReceivedWithAnyArgs(1).SendAsync("");
    }

    [Fact]
    public async Task ShouldStorePendingMessageWhenDestinationConnectionIdNull()
    {
        // Arrange
        _hub.Content = [0x01, 0x02, 0x03];
        _hub.Next = PKey.Address2;
        _hub.DestinationConnectionId = null;

        // Act
        var result = await _hub.RouteMessage(new());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SuccessResult<bool>>();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().BeTrue();
        var pendingMessage = await _dbContext.Messages.FirstOrDefaultAsync(item => item.Destination == PKey.Address2 && item.Content == "AQID");
        pendingMessage.Should().NotBeNull();
        _hub.Clients.DidNotReceiveWithAnyArgs().Client("");
    }

    [Fact]
    public async Task ShouldNotRouteMessageWhenDestinationConnectionIdAndContentNull()
    {
        // Arrange
        _hub.DestinationConnectionId = null;
        _hub.Content = null;

        // Act
        var result = await _hub.RouteMessage(new());

        result.Should().NotBeNull();
        result.Should().BeOfType<ErrorResult<bool>>();
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.Single().Message.Should().Be(InvocationErrors.ONION_ROUTING_FAILED);
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldStorePendingMessageWhenSendAsyncFails()
    {
        // Arrange
        var singleClientProxy = Substitute.For<ISingleClientProxy>();
        singleClientProxy.SendAsync("", default).ThrowsForAnyArgs(new Exception("client not reachable"));
        _hub.Clients.Client(_testConnectionId2).Returns(singleClientProxy);
        _hub.Content = [0x01, 0x02, 0x03];
        _hub.Next = PKey.Address2;
        _hub.DestinationConnectionId = _testConnectionId2;

        // Act
        var result = await _hub.RouteMessage(new());

        // Assert
        var pendingMessage = await _dbContext.Messages.FirstOrDefaultAsync(item => item.Destination == PKey.Address2 && item.Content == "AQID");
        pendingMessage.Should().NotBeNull();
    }

    #endregion ROUTE_MESSAGE

    #region ON_DISCONNECTED

    [Fact]
    public async Task ShouldLogOutWhenClientDisconneced()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateLocalAdjacencyCommand>()).ReturnsForAnyArgs(Task.FromResult(CommandResult.CreateResultSuccess(new VertexBroadcastRequest())));
        var graph = Substitute.For<Enigma5.App.Data.NetworkGraph>(_container.Resolve<IEnvelopeSigner>(), _certificateManager, _configuration, Substitute.For<ILogger<Enigma5.App.Data.NetworkGraph>>());
        graph.NeighboringAddresses.Returns([PKey.Address1]);
        var hub = new RoutingHub(_sessionManager, _certificateManager, graph, mediator, Substitute.For<ILogger<RoutingHub>>());
        ConfigureSignalRHub(hub);

        // Act
        await hub.OnDisconnectedAsync(null);

        // Assert
        _sessionManager.Received(1).Remove(_testConnectionId1, out Arg.Any<string?>());
        hub.Clients.Received(1).Client(_testConnectionId1);
        await hub.Clients.Client(_testConnectionId1).ReceivedWithAnyArgs(1).SendAsync("");
    }

    [Fact]
    public async Task ShouldLogWarningWhenUpdateAdjacencyCommandFails()
    {
        // Arrange
        var logger = Substitute.For<ILogger<RoutingHub>>();
        _sessionManager.Remove("", out string? _).ReturnsForAnyArgs(false);
        var hub = new RoutingHub(_sessionManager, _certificateManager, _graph, _mediator, logger);
        ConfigureSignalRHub(hub);

        // Act
        await hub.OnDisconnectedAsync(null);

        // Assert
        _sessionManager.Received(1).Remove(_testConnectionId1, out Arg.Any<string?>());
        hub.Clients.DidNotReceiveWithAnyArgs().Client("");
        logger.ReceivedWithAnyArgs(2).LogWarning("");
    }

    #endregion ON_DISCONNECTED
}
