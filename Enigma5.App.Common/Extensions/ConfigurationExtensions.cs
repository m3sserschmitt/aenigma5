/*
    Aenigma - Onion Routing based messaging application
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

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
    => configuration.GetValue<string?>("PassphrasePath", null);

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
