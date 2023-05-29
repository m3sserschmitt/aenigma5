using Xunit;

namespace Enigma5.Message.Tests;

public class AddressContextTests
{
    [Theory(Skip = "Skipped because AddressContext is not thread static.")]
    [InlineData(16)]
    [InlineData(64)]
    [InlineData(128)]
    public void AddressContext_ShouldUpdateAddressSizeAccordingly(int newAddressSize)
    {
        // Arrange
        using (new AddressContext(AddressContext.DefaultAddressSize))
        {
            int updated;

            // Act
            using (new AddressContext(newAddressSize))
            {
                updated = AddressContext.Current.AddressSize;
            }

            // Assert
            Assert.Equal(AddressContext.DefaultAddressSize, AddressContext.Current.AddressSize);
            Assert.Equal(newAddressSize, updated);
        }

    }
}
