using Enigma5.Crypto.DataProviders;
using Xunit;

namespace Enigma5.Crypto.Tests;

public class CertificateHelperTests
{
    [Fact]
    public void CertificateHelper_ShouldComputeCorrectAddressFromPublicKey()
    {
        // Arrange

        // Act
        var address = CertificateHelper.GetHexAddressFromPublicKey(PKey.PublicKey1);

        // Assert
        Assert.Equal(PKey.Address1, address);
    }
}
