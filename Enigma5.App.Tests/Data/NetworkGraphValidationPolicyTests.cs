
using Enigma5.App.Data;

namespace Enigma5.App.Tests.Data;

public class NetworkGraphValidationPolicyTests : AppTestBase
{
    [Fact]
    public void ShouldValidateVertex()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex();

        // Act
        var valid = NetworkGraphValidationPolicy.ValidatePolicy(vertex);

        // Assert
        valid.Should().BeTrue();
    }

    [Fact]
    public void ShouldNotValidateVertexWithInvalidSignature()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex();
        vertex.SignedData = "naoif9823-409gfsakjdigu908";

        // Act
        var valid = NetworkGraphValidationPolicy.ValidatePolicy(vertex);

        // Assert
        valid.Should().BeFalse();
    }

    [Fact]
    public void ShouldNotValidateVertexWithInvalidPublicKey()
    {
        // Arrange
        var vertex = _scope.ResolveAdjacentVertex();
        vertex.PublicKey = "naoif9823-409gfsakjdigu908";

        // Act
        var valid = NetworkGraphValidationPolicy.ValidatePolicy(vertex);

        // Assert
        valid.Should().BeFalse();
    }

    [Fact]
    public void ShouldNotValidateVertexWithCycle()
    {
        // Arrange
        var address = _scope.ResolveAdjacentVertex().Neighborhood.Address;
        var vertex = _scope.ResolveAdjacentVertex(new List<string> { address });

        // Act
        var valid = NetworkGraphValidationPolicy.ValidatePolicy(vertex);

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
        var valid = NetworkGraphValidationPolicy.ValidatePolicy(vertex);

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
        var valid = NetworkGraphValidationPolicy.ValidatePolicy(vertex);

        // Assert
        valid.Should().BeFalse();
    }
}
