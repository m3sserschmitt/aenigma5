/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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
using Enigma5.App.Hubs;
using Enigma5.App.Hubs.Filters;
using Enigma5.App.Models.HubInvocation;
using Enigma5.Crypto.DataProviders;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Enigma5.App.Tests.Hubs.Filters;

[ExcludeFromCodeCoverage]
public class OnionRoutingFilterTests : FiltersTestBase<OnionRoutingFilter>
{
    [Fact]
    public async Task ShouldMapToConnectionId()
    {
        // Arrange
        _hub.Next = PKey.Address2;

        // Act
        await _filter.Handle(_hubInvocationContext, _next);

        // Assert
        _hub.DestinationConnectionId.Should().Be("test-connection-id-2");
        await _next.Received(1)(_hubInvocationContext);
    }

    [Fact]
    public async Task ShouldNotMapToConnectionIdForNullNextAddress()
    {
        // Arrange
        _hub.Next = null;

        // Act
        var result = await _filter.Handle(_hubInvocationContext, _next);

        // Assert
        _hub.DestinationConnectionId.Should().BeNull();
        var response = result as EmptyErrorResult;
        response.Should().NotBeNull();
        response!.Errors.Should().HaveCount(1);
        response.Errors.Single().Message.Should().Be(InvocationErrors.ONION_ROUTING_FAILED);
        await _next.DidNotReceiveWithAnyArgs()(_hubInvocationContext);
    }

    [Fact]
    public async Task ShouldNotMapToConnectionIdWhenNotAuthenticated()
    {
        // Arrange
        _hub.Next = PKey.Address2;
        _sessionManager.TryGetConnectionId(Arg.Any<string>(), out Arg.Any<string?>()).Returns(args => {
            args[1] = null;
            return false;
        });

        // Act
        await _filter.Handle(_hubInvocationContext, _next);

        // Assert
        _hub.DestinationConnectionId.Should().BeNull();
        await _next.Received(1)(_hubInvocationContext);
    }
}
