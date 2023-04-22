using Crypto;
using System.Security.Cryptography;
using System.Text;
using Xunit;

public class AesGcmEncryptionTests
{
    public class AesGcmEncryptorTests
    {
        [Fact]
        public void TestAesGcmEncryptDecrypt()
        {
            // Arrange
            byte[] key = new byte[32];
            RandomNumberGenerator.Fill(key);
            byte[] data = Encoding.UTF8.GetBytes("test data");

            // Act
            byte[] encrypted = AesGcmEncryptor.AesGcm256Encrypt(key, data);
            byte[] decrypted = AesGcmEncryptor.AesGcm256Decrypt(key, encrypted);

            // Assert
            Assert.Equal(data, decrypted);
        }
    }
}
