using Enigma5.Security.Contracts;
using Microsoft.Extensions.Configuration;
using Enigma5.App.Common.Extensions;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Enigma5.Security;

public class AzureKeysReader(
    IConfiguration configuration,
    AzureClient azureClient,
    ILogger<AzureKeysReader> logger) : IKeysReader
{
    private static readonly string PUBLIC_KEY_SECRET_NAME_NOT_PROVIDED = "Public key secret name not provided.";

    private static readonly string PRIVATE_KEY_SECRET_NAME_NOT_PROVIDED = "Private key secret name not provided.";

    private readonly IConfiguration _configuration = configuration;

    private readonly ILogger<AzureKeysReader> _logger = logger;

    private readonly AzureClient _azureClient = azureClient;

    public byte[] PrivateKey => ReadPrivateKey();

    public string PublicKey => ReadPublicKey();

    private string ReadPublicKey()
    {
        try
        {
            var publicKeyPath = _configuration.GetPublicKeyPath() ?? throw new Exception(PUBLIC_KEY_SECRET_NAME_NOT_PROVIDED);
            return _azureClient.GetSecret(publicKeyPath);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error occurred while reading public key.");
            throw;
        }
    }

    private byte[] ReadPrivateKey()
    {
        try
        {
            var publicKeyPath = _configuration.GetPrivateKeyPath() ?? throw new Exception(PRIVATE_KEY_SECRET_NAME_NOT_PROVIDED);
            return Encoding.UTF8.GetBytes(_azureClient.GetSecret(publicKeyPath));
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error occurred while reading private key.");
            throw;
        }
    }
}
