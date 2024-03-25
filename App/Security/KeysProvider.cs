using System.Text;
using Enigma5.App.Common.Extensions;
using Enigma5.App.Security.Contracts;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Enigma5.App.Security;

public class KeysProvider(IPassphraseProvider passphraseProvider, IConfiguration configuration)
{
    private const string PUBLIC_KEY_NOT_CONFIGURED_ERROR_MESSAGE = "Public Key file not configured.";

    private const string PRIVATE_KEY_NOT_CONFIGURED_ERROR_MESSAGE = "Private Key file not configured.";

    private const string PRIVATE_KEY_FILE_NOT_FOUND_ERROR_MESSAGE = "Private Key file not found.";

    private const string PUBLIC_KEY_NOT_FOUND_ERROR_MESSAGE = "Public Key file not found.";

    private const string INVALID_PRIVATE_KEY_PEM_OBJECT = "Private key PEM is invalid.";
    
    public string PublicKeyPath { get; } = configuration.GetPublicKeyPath() ?? throw new Exception(PUBLIC_KEY_NOT_CONFIGURED_ERROR_MESSAGE);

    public string PrivateKeyPath { get; } = configuration.GetPrivateKeyPath() ?? throw new Exception(PRIVATE_KEY_NOT_CONFIGURED_ERROR_MESSAGE);

    public bool PublicKeyFileExists = File.Exists(configuration.GetPublicKeyPath());

    public bool PrivateKeyFileExists = File.Exists(configuration.GetPrivateKeyPath());

    public byte[] PrivateKey => ExportToPem(ReadPrivateKeyFile());

    public string PublicKey => ReadPublicKeyFile();

    private string ReadPublicKeyFile()
    {
        if(!PublicKeyFileExists)
        {
            throw new Exception(PUBLIC_KEY_NOT_FOUND_ERROR_MESSAGE);
        }

        return File.ReadAllText(PublicKeyPath);
    }

    private AsymmetricKeyParameter ReadPrivateKeyFile()
    {
        if(!PrivateKeyFileExists)
        {
            throw new Exception(PRIVATE_KEY_FILE_NOT_FOUND_ERROR_MESSAGE);
        }

        using var reader = File.OpenText(PrivateKeyPath);
        using var pemReader = new PemReader(reader);

        var pemObject = pemReader.ReadPemObject()
        ?? throw new Exception(INVALID_PRIVATE_KEY_PEM_OBJECT);

        reader.Close();
        pemReader.Reader.Close();
        var passphrase = passphraseProvider.ProvidePassphrase();
        var keyParameter = PrivateKeyFactory.DecryptKey(passphrase, pemObject.Content);
        Array.Clear(passphrase);

        return keyParameter;
    }

    private static byte[] ExportToPem(AsymmetricKeyParameter key)
    {
        var stringBuilder = new StringBuilder();
        using var stringWriter = new StringWriter(stringBuilder);
        using var pemWriter = new PemWriter(stringWriter);

        pemWriter.WriteObject(key);
        
        pemWriter.Writer.Flush();
        pemWriter.Writer.Close();
        stringWriter.Flush();
        stringWriter.Close();

        var keyMaterial = new char[stringBuilder.Length];
        stringBuilder.CopyTo(0, keyMaterial, 0, stringBuilder.Length);
        stringBuilder.Clear();

        var keyBytes = Encoding.UTF8.GetBytes(keyMaterial);
        Array.Clear(keyMaterial);

        return keyBytes;
    }
}
