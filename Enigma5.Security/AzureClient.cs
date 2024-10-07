using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Enigma5.App.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Enigma5.Security;

public class AzureClient(IConfiguration configuration, ILogger<AzureKeysReader> logger)
{
    private static readonly string KEYS_LOCATION_NOT_PROVIDED = "Keys location url not provided.";

    private readonly IConfiguration _configuration = configuration;

    private readonly ILogger<AzureKeysReader> _logger = logger;

    private string Url
    {
        get
        {
            try
            {
                return _configuration.GetAzureVaultUrl() ?? throw new Exception(KEYS_LOCATION_NOT_PROVIDED);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fatal error encountered while trying to retrieve keys location from config.");
                throw;
            }
        }
    }

    private SecretClient SecretClient => new(new Uri(Url), new DefaultAzureCredential());

    public string GetSecret(string name, string? version = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return SecretClient.GetSecret(name, version, cancellationToken).Value.Value; ;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error encountered while trying to read secret from vault.");
            throw;
        }
    }
}
