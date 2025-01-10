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
using Enigma5.Crypto.DataProviders;
using Enigma5.Tests.Base;
using FluentAssertions;

namespace Enigma5.App.Models.Tests;

[ExcludeFromCodeCoverage]
public class VertexBroadcastRequestTests
{
    [Fact]
    public void ShouldValidate()
    {
        // Arrange
        var request = DataSeeder.ModelsFactory.VertexBroadcastRequest;

        // Act
        var result = request.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotValidateForNullPublicKey()
    {
        // Arrange
        var request = new VertexBroadcastRequest(null, DataSeeder.ModelsFactory.VertexBroadcastRequest.SignedData);

        // Act
        var result = request.Validate();

        // Assert
        result.Should().HaveCount(2);
        var firstError = result.First();
        firstError.Message.Should().Be(ValidationErrors.NULL_REQUIRED_PROPERTIES);
        firstError.Properties.Should().HaveCount(1);
        firstError.Properties.Should().Contain(nameof(request.PublicKey));
        var lastError = result.Last();
        lastError.Message.Should().Be(ValidationErrors.PROPERTIES_FORMAT_COULD_NOT_BE_VERIFIED);
        lastError.Properties.Should().HaveCount(1);
        lastError.Properties.Should().Contain(nameof(request.SignedData));
    }

    [Fact]
    public void ShouldNotValidateForInvalidPublicKey()
    {
        // Arrange
        var request = new VertexBroadcastRequest("  --- invalid-public-key ---  ", DataSeeder.ModelsFactory.VertexBroadcastRequest.SignedData);

        // Act
        var result = request.Validate();

        // Assert
        result.Should().HaveCount(2);
        var firstError = result.First();
        firstError.Message.Should().Be(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT);
        firstError.Properties.Should().HaveCount(1);
        firstError.Properties.Should().Contain(nameof(request.PublicKey));
        var lastError = result.Last();
        lastError.Message.Should().Be(ValidationErrors.PROPERTIES_FORMAT_COULD_NOT_BE_VERIFIED);
        lastError.Properties.Should().HaveCount(1);
        lastError.Properties.Should().Contain(nameof(request.SignedData));
    }

    [Fact]
    public void ShouldNotValidateForInvalidSignedData()
    {
        // Arrange
        var request = new VertexBroadcastRequest(PKey.PublicKey1, "invalid-signed-data");

        // Act
        var result = request.Validate();

        // Assert
        result.Should().HaveCount(1);
        var firstError = result.Single();
        firstError.Message.Should().Be(ValidationErrors.PROPERTIES_FORMAT_COULD_NOT_BE_VERIFIED);
        firstError.Properties.Should().HaveCount(1);
        firstError.Properties.Should().Contain(nameof(request.SignedData));
    }
}
