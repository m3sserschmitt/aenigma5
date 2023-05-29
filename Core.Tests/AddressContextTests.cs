using Xunit;

namespace Enigma5.Core.Tests;

public class AddressContextTests
{
    [Theory]
    [InlineData(16)]
    [InlineData(64)]
    [InlineData(128)]
    public void AddressContext_ShouldUpdateAddressSizeAccordingly(int newAddressSize)
    {
        // Arrange
        int previous = AddressContext.Current.AddressSize;

        // Act
        int updated;
        using (new AddressContext(newAddressSize))
        {
            updated = AddressContext.Current.AddressSize;
        }

        // Assert
        Assert.Equal(AddressContext.Current.AddressSize, previous);
        Assert.Equal(newAddressSize, updated);
    }
}
