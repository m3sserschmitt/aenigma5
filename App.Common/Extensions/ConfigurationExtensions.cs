using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Common.Extensions;

public static class ConfigurationExtensions
{
    public static IList<string>? GetPeers(this IConfiguration configuration)
    => configuration.GetSection("Peers").Get<List<string>>();

    public static string? GetLocalListenAddress(this IConfiguration configuration)
    => configuration.GetValue<string>("Kestrel:EndPoints:Http:Url");

    public static string? GetHostname(this IConfiguration configuration)
    => configuration.GetValue<string>("Hostname");
}
