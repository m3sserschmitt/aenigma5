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

using Enigma5.App.Data;
using Enigma5.Crypto.DataProviders;
using Enigma5.App.Tests.Helpers;
using Xunit;
using FluentAssertions;

namespace Enigma5.App.Tests.Data;

public class NetworkGraphValidationPolicyTests : AppTestBase
{
    [Fact]
    public void ShouldValidateVertex()
    {
        // Arrange
        var vertex = _container.ResolveAdjacentVertex();

        // Act
        var valid = vertex.ValidatePolicy();

        // Assert
        valid.Should().BeTrue();
    }

    [Fact]
    public void ShouldNotValidateVertexWithInvalidSignature()
    {
        // Arrange
        var vertex = new Vertex(new ([], PKey.Address1, null), PKey.PublicKey1, "fake-signature");

        // Act
        var valid = vertex.ValidateSignature();

        // Assert
        valid.Should().BeFalse();
    }

    [Fact]
    public void ShouldNotValidateVertexWithInvalidPublicKey()
    {
        // Arrange
        var vertex = new Vertex(new ([], PKey.Address1, null), "fake-public-key", "fake-signature");

        // Act
        var valid = vertex.ValidatePublicKey();

        // Assert
        valid.Should().BeFalse();
    }

    [Fact]
    public void ShouldNotValidateVertexWithCycle()
    {
        // Arrange
        var address = _container.ResolveAdjacentVertex().Neighborhood.Address;
        var vertex = _container.ResolveAdjacentVertex([address]);

        // Act
        var valid = vertex.CheckCycles();

        // Assert
        valid.Should().BeFalse();
    }

    [Fact]
    public void ShouldNotValidateVertexWithModifiedNeighborhood()
    {
        // Arrange
        var vertex = _container.ResolveAdjacentVertex();
        vertex.Neighborhood.Neighbors.Add("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff");

        // Act
        var valid = vertex.ValidateSignature();

        // Assert
        valid.Should().BeFalse();
    }

    [Fact]
    public void ShouldNotValidateVertexWithInvalidNeighborAddress()
    {
        // Arrange
        var invalidAddresses = new HashSet<string> { "fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffxyz" };
        var vertex = _container.ResolveAdjacentVertex(invalidAddresses);

        // Act
        var valid = vertex.ValidateNeighborsAddresses();

        // Assert
        valid.Should().BeFalse();
    }
}
