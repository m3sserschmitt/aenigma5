using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Common.Extensions;

public static class ConfigurationExtensions
{
    public static List<string>? GetPeers(this IConfiguration configuration)
    => configuration.GetSection("Peers").Get<List<string>>();

    public static string? GetLocalListenAddress(this IConfiguration configuration)
    => configuration.GetValue<string>("Kestrel:EndPoints:Http:Url");

    public static string? GetHostname(this IConfiguration configuration)
    => configuration.GetValue<string>("Hostname");

    public static string? GetPrivateKeyPath(this IConfiguration configuration)
    => configuration.GetValue<string>("PrivateKeyPath");

    public static string? GetPublicKeyPath(this IConfiguration configuration)
    => configuration.GetValue<string>("PublicKeyPath");

    public static bool GetRetryConnection(this IConfiguration configuration)
    => configuration.GetValue<bool>("RetryConnection");

    public static int GetConnectionRetriesCount(this IConfiguration configuration)
    => configuration.GetValue<int>("ConnectionRetriesCount");

    public static int GetDelayBetweenConnectionRetries(this IConfiguration configuration)
    => configuration.GetValue<int>("DelayBetweenConnectionRetries");
}
