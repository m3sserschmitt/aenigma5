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
using Enigma5.App.Hubs.Sessions;
using Enigma5.App.Tests.Helpers;
using Enigma5.Crypto.DataProviders;
using Enigma5.Crypto.Extensions;
using FluentAssertions;
using Xunit;

namespace Enigma5.App.Tests.Hubs.Sessions;

[ExcludeFromCodeCoverage]
public class SessionManagerTests
{
    [Fact]
    public void ShouldAddPending()
    {
        // Arrange
        var sessionManager = new SessionManager(new());

        // Act
        var nonce = sessionManager.AddPending("test-connection-id");

        // Assert
        sessionManager.Pending.TryGetValue("test-connection-id", out string? value).Should().BeTrue();
        sessionManager.Authenticated.Should().BeEmpty();
        sessionManager.ConnectionsMapper.Connections.Should().BeEmpty();
        value.IsValidBase64().Should().BeTrue();
        nonce.Should().Be(value);
    }

    [Fact]
    public void ShouldNotAddPendingTwice()
    {
        // Arrange
        var sessionManager = new SessionManager(new());

        // Act
        var nonce1 = sessionManager.AddPending("test-connection-id");
        var nonce2 = sessionManager.AddPending("test-connection-id");

        // Assert
        sessionManager.Pending.TryGetValue("test-connection-id", out string? value).Should().BeTrue();
        sessionManager.Authenticated.Should().BeEmpty();
        sessionManager.ConnectionsMapper.Connections.Should().BeEmpty();
        value.IsValidBase64().Should().BeTrue();
        nonce1.Should().Be(value);
        nonce2.Should().BeNull();
    }

    [Fact]
    public void ShouldAuthenticate()
    {
        // Arrange
        var sessionManager = new SessionManager(new());
        var nonce = sessionManager.AddPending("test-connection-id");
        var request = DataSeeder.ModelsFactory.CreateAuthenticationRequest(nonce!);
        
        // Act
        var authenticated = sessionManager.Authenticate("test-connection-id", request.PublicKey!, request.Signature!);
    
        // Assert
        authenticated.Should().BeTrue();
        sessionManager.Pending.Should().BeEmpty();
        sessionManager.Authenticated.Should().Contain("test-connection-id");
        sessionManager.ConnectionsMapper.Connections.TryGetValue(PKey.Address1, out string? connectionId).Should().BeTrue();
        connectionId.Should().Be("test-connection-id");
    }

    [Fact]
    public void ShouldNotAuthenticateTwice()
    {
        // Arrange
        var sessionManager = new SessionManager(new());
        var nonce = sessionManager.AddPending("test-connection-id");
        var request = DataSeeder.ModelsFactory.CreateAuthenticationRequest(nonce!);
        
        // Act
        var authenticated1 = sessionManager.Authenticate("test-connection-id", request.PublicKey!, request.Signature!);
        var authenticated2 = sessionManager.Authenticate("test-connection-id", request.PublicKey!, request.Signature!);
    
        // Assert
        authenticated1.Should().BeTrue();
        authenticated2.Should().BeFalse();
        sessionManager.Pending.Should().BeEmpty();
        sessionManager.Authenticated.Should().Contain("test-connection-id");
        sessionManager.ConnectionsMapper.Connections.TryGetValue(PKey.Address1, out string? connectionId).Should().BeTrue();
        connectionId.Should().Be("test-connection-id");
    }
}
