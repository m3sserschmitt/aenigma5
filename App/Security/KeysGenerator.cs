using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using Enigma5.Core;

namespace Enigma5.App.Security;

public static class KeysGenerator
{
    public static (string PublicKey, string PrivateKey) GenerateKeys()
    {
        var keyPair = GenerateRsaKeyPair();

        string publicKeyPem = ExportToPem(keyPair.Public);
        string privateKeyPem = ExportToPem(keyPair.Private);

        return (publicKeyPem, privateKeyPem);
    }

    private static AsymmetricCipherKeyPair GenerateRsaKeyPair()
    {
        var generator = new RsaKeyPairGenerator();
        var keyGenParam = new KeyGenerationParameters(
            new SecureRandom(),
            PKeySize.Value
        );
        generator.Init(keyGenParam);
        return generator.GenerateKeyPair();
    }

    private static string ExportToPem(AsymmetricKeyParameter key)
    {
        using var stringWriter = new StringWriter();
        var pemWriter = new PemWriter(stringWriter);
        pemWriter.WriteObject(key);
        return stringWriter.ToString();
    }
}
