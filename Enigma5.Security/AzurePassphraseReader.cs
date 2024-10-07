using Enigma5.App.Common.Extensions;
using Enigma5.App.Security.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Enigma5.Security;

public class AzurePassphraseReader(
    IConfiguration configuration,
    AzureClient azureClient,
    ILogger<AzurePassphraseReader> logger) : IPassphraseProvider
{
    private static readonly string PASSPHRASE_NAME_NOT_PROVIDED = "Passphrase name not provided";

    private readonly IConfiguration _configuration = configuration;

    private readonly ILogger<AzurePassphraseReader> _logger = logger;

    private readonly AzureClient _azureClient = azureClient;

    public char[] ProvidePassphrase()
    {
        try
        {
            var passphrasePath = _configuration.GetPassphraseKeyPath() ?? throw new Exception(PASSPHRASE_NAME_NOT_PROVIDED);
            return _azureClient.GetSecret(passphrasePath).ToCharArray();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error encountered while trying to read passphrase from vault.");
            throw;
        }
    }
}
