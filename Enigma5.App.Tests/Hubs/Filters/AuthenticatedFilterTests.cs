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
using Enigma5.App.Hubs;
using Enigma5.App.Hubs.Filters;
using Enigma5.App.Models.HubInvocation;
using Enigma5.Crypto.DataProviders;
using FluentAssertions;
using Xunit;

namespace Enigma5.App.Tests.Hubs.Filters;

[ExcludeFromCodeCoverage]
public class AuthenticatedFilterTests : FiltersTestBase<AuthenticatedFilter>
{
    [Fact]
    public async Task ShouldResolveClientAddress()
    {
        // Arrange
        _connectionMapper.TryAdd(PKey.Address1, "test-connection-id");
        
        // Act
        await _filter.Handle(_hubInvocationContext, _ => ValueTask.FromResult<object?>(default));

        // Assert
        _hub.ClientAddress.Should().Be(PKey.Address1);
    }

    [Fact]
    public async Task ShouldNotResolveNotExistentConnectionId()
    {
        // Arrange
        _connectionMapper.TryAdd(PKey.Address1, "test-connection-id-2");
        
        // Act
        var result = await _filter.Handle(_hubInvocationContext, _ => ValueTask.FromResult<object?>(default));

        // Assert
        _hub.ClientAddress.Should().BeNull();
        var response = result as EmptyErrorResult;
        response.Should().NotBeNull();
        response!.Errors.Should().HaveCount(1);
        response.Errors.Single().Message.Should().Be(InvocationErrors.ONION_PARSING_FAILED);
    }
}
