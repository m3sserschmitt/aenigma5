using Enigma5.Core;
using Xunit;

namespace Enigma5.Core.Tests;

public class PKeyContextContextTests
{
    [Theory]
    [InlineData(16)]
    [InlineData(64)]
    [InlineData(128)]
    public void PKeyContext_ShouldUpdateAddressSizeAccordingly(int newAddressSize)
    {
        // Arrange
        int previous = PKeySize.Current.Value;

        // Act
        int updated;
        using (new PKeySize(newAddressSize))
        {
            updated = PKeySize.Current.Value;
        }

        // Assert
        Assert.Equal(PKeySize.Current.Value, previous);
        Assert.Equal(newAddressSize, updated);
    }

    [Fact]
    public void PKeyContext_ShouldReturnDefaultValue()
    {
        // Arrange

        // Act
        var pkeySize = PKeySize.Current.Value;

        // Assert
        Assert.Equal(2048, pkeySize);
    }
}
