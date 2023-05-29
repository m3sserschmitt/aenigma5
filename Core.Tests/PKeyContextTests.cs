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
        int previous = PKeyContext.Current.PKeySize;

        // Act
        int updated;
        using (new PKeyContext(newAddressSize))
        {
            updated = PKeyContext.Current.PKeySize;
        }

        // Assert
        Assert.Equal(PKeyContext.Current.PKeySize, previous);
        Assert.Equal(newAddressSize, updated);
    }
}
