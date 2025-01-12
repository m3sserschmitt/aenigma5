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
using FluentAssertions;

namespace Enigma5.App.Models.Tests;

[ExcludeFromCodeCoverage]
public class RoutingRequestTests
{
    [Fact]
    public void ShouldValidate()
    {
        // Arrange
        var request = new RoutingRequest("dGVzdC1zdHJpbmc=");

        // Act
        var result = request.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotValidateNullPayload()
    {
        // Arrange
        var request = new RoutingRequest(null);

        // Act
        var result = request.Validate();

        // Assert
        result.Should().HaveCount(1);
        var error = result.Single();
        error.Message.Should().Be(ValidationErrors.NULL_REQUIRED_PROPERTIES);
        error.Properties.Should().HaveCount(1);
        error.Properties!.Single().Should().Be(nameof(request.Payload));
    }

    [Fact]
    public void ShouldNotValidateEmptyPayload()
    {
        // Arrange
        var request = new RoutingRequest("   ");

        // Act
        var result = request.Validate();

        // Assert
        result.Should().HaveCount(1);
        var error = result.Single();
        error.Message.Should().Be(ValidationErrors.NULL_REQUIRED_PROPERTIES);
        error.Properties.Should().HaveCount(1);
        error.Properties!.Single().Should().Be(nameof(request.Payload));
    }

    [Fact]
    public void ShouldNotValidateInvalidBase64()
    {
        // Arrange
        var request = new RoutingRequest("invalid-base64");

        // Act
        var result = request.Validate();

        // Assert
        result.Should().HaveCount(1);
        var error = result.Single();
        error.Message.Should().Be(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT);
        error.Properties.Should().HaveCount(1);
        error.Properties!.Single().Should().Be(nameof(request.Payload));
    }
}