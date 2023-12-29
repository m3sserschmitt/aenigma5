using Xunit;

namespace Enigma5.Core.Tests;

public class AddressSizeTests
{
    [Theory]
    [InlineData(16)]
    [InlineData(64)]
    [InlineData(128)]
    public void AddressSize_ShouldUpdateAddressSizeAccordingly(int newAddressSize)
    {
        // Arrange
        int previous = AddressSize.Current.Value;

        // Act
        int updated;
        using (new AddressSize(newAddressSize))
        {
            updated = AddressSize.Current.Value;
        }

        // Assert
        Assert.Equal(AddressSize.Current.Value, previous);
        Assert.Equal(newAddressSize, updated);
    }

    [Fact]
    public void AddressSize_ShouldReturnDefaultValue()
    {
        // Arrange

        // Act
        var addressSize = AddressSize.Current.Value;

        // Assert
        Assert.Equal(32, addressSize);
    }
}
