using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Common.Extensions;

public static class ConfigurationExtensions
{
    public static List<string> GetPeers(this IConfiguration configuration)
    => configuration.GetSection("Peers").Get<List<string>>() ?? [];

    public static string? GetLocalListenAddress(this IConfiguration configuration)
    => configuration.GetValue<string?>("Kestrel:EndPoints:Http:Url", null);

    public static string? GetHostname(this IConfiguration configuration)
    => configuration.GetValue<string?>("Hostname", null);

    public static string? GetPrivateKeyPath(this IConfiguration configuration)
    => configuration.GetValue<string?>("PrivateKeyPath", null);

    public static string? GetPublicKeyPath(this IConfiguration configuration)
    => configuration.GetValue<string?>("PublicKeyPath", null);

    public static string? GetPassphraseKeyPath(this IConfiguration configuration)
    => configuration.GetValue<string?>("Passphrase", null);

    public static bool GetRetryConnection(this IConfiguration configuration)
    => configuration.GetValue("RetryConnection", false);

    public static int GetConnectionRetriesCount(this IConfiguration configuration)
    => configuration.GetValue("ConnectionRetriesCount", 0);

    public static int GetDelayBetweenConnectionRetries(this IConfiguration configuration)
    => configuration.GetValue("DelayBetweenConnectionRetries", 0);

    public static TimeSpan? GetNonActiveLeafsLifetime(this IConfiguration configuration)
    {
        var value = configuration.GetValue<string?>("NonActiveLeafsLifetime", null);

        if (value is null)
        {
            return null;
        }

        if (TimeSpan.TryParse(value, out var timeSpan))
        {
            return timeSpan;
        }

        return null;
    }

    public static string? GetAzureVaultUrl(this IConfiguration configuration)
    => configuration.GetValue<string?>("AzureVaultUrl", null);

    public static bool UseAzureVaultForKeys(this IConfiguration configuration)
    => configuration.GetValue("UseAzureVaultForKeys", false);

    public static bool UseAzureVaultForPassphrase(this IConfiguration configuration)
    => configuration.GetValue("UseAzureVaultForPassphrase", false);
}
