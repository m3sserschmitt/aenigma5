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
using Enigma5.App.Resources.Handlers;
using Enigma5.App.Resources.Queries;
using Enigma5.Crypto.DataProviders;
using FluentAssertions;
using Xunit;

namespace Enigma5.App.Tests.Resources.Handlers;

[ExcludeFromCodeCoverage]
public class CheckAuthorizedServiceHandlerTests : HandlerTestBase<CheckAuthorizedServiceHandler>
{
    [Fact]
    public async Task ShouldAuthorize()
    {
        // Arrange
        var request = new CheckAuthorizedServiceQuery(PKey.Address1);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<CommandResult<bool>>();
        result.Success.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotAuthorize()
    {
        // Arrange
        var request = new CheckAuthorizedServiceQuery(PKey.Address2);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<CommandResult<bool>>();
        result.Success.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldFailAuthorizationForInvalidAddress()
    {
        // Arrange
        var request = new CheckAuthorizedServiceQuery("invalid-address");

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<CommandResult<bool>>();
        result.Success.Should().BeFalse();
        result.Value.Should().BeFalse();
    }
}
