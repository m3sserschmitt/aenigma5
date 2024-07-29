
using Enigma5.App.Data;
using Enigma5.Crypto.DataProviders;

namespace Enigma5.App.Tests.Data;

public class NetworkGraphValidationPolicyTests : AppTestBase
{
    [Fact]
    public void ShouldValidateVertex()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex();

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
        var address = _scope.ResolveAdjacentVertex().Neighborhood.Address;
        var vertex = _scope.ResolveAdjacentVertex([address]);

        // Act
        var valid = vertex.CheckCycles();

        // Assert
        valid.Should().BeFalse();
    }

    [Fact]
    public void ShouldNotValidateVertexWithModifiedNeighborhood()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex();
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
        var invalidAddresses = new List<string> { "fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffxyz" };
        var vertex = _scope.ResolveAdjacentVertex(invalidAddresses);

        // Act
        var valid = vertex.ValidateNeighborsAddresses();

        // Assert
        valid.Should().BeFalse();
    }
}
