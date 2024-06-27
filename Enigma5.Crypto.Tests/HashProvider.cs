using System.Text;
using Xunit;

namespace Enigma5.Crypto.Tests;

public class HashProviderTests
{
    [Theory]
    [InlineData("test", "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")]
    [InlineData("test 123", "f7ef53d21502321eaecb78bb405b7ff266253b4a27d89b9b8c4da5847cdd1b9d")]
    [InlineData("test 123 123", "97155afa49dc5a5c468c259306d22a95eddd9ae257d9ccf3356f364c447e4907")]
    public void HashProvider_ShouldProduceCorrectHash(string input, string expected)
    {
        // Arrange

        // Act
        var actual = HashProvider.Sha256Hex(Encoding.UTF8.GetBytes(input));

        // Assert
        Assert.Equal(expected, actual);
    }
}
