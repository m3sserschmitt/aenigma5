/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

namespace Enigma5.App.Common.Extensions;

public static class ConfigurationExtensions
{
    public static string? GetLocalListenAddress(this IConfiguration configuration)
    => configuration.GetValue<string?>("Kestrel:EndPoints:Http:Url", null);

    public static string? GetHostname(this IConfiguration configuration)
    => configuration.GetValue<string?>("Hostname", null)?.Trim('/', ' ');

    public static string? GetPrivateKeyPath(this IConfiguration configuration)
    => configuration.GetValue<string?>("PrivateKeyPath", null);

    public static string? GetPublicKeyPath(this IConfiguration configuration)
    => configuration.GetValue<string?>("PublicKeyPath", null);

    public static string? GetWebContentDirectory(this IConfiguration configuration)
    => configuration.GetValue<string?>("WebContentDirectory");

    public static TimeSpan GetMessageRetentionPeriod(this IConfiguration condiguration)
    => condiguration.GetTimeSpan("MessageRetentionPeriod", new(0));

    public static TimeSpan GetSentMessageRetentionPeriod(this IConfiguration condiguration)
    => condiguration.GetTimeSpan("SentMessageRetentionPeriod", new(0));

    public static TimeSpan GetSharedDataRetentionPeriod(this IConfiguration condiguration)
    => condiguration.GetTimeSpan("SharedDataRetentionPeriod", new(0));

    public static TimeSpan GetFilesRetentionPeriod(this IConfiguration configuration)
    => configuration.GetTimeSpan("FilesRetentionPeriod", new(0));

    public static string? GetPassphraseKeyPath(this IConfiguration configuration)
    => configuration.GetValue<string?>("PassphrasePath", null);

    public static bool GetRetryConnection(this IConfiguration configuration)
    => configuration.GetValue("Network:RetryConnections", false);

    public static int GetConnectionRetriesCount(this IConfiguration configuration)
    => configuration.GetValue("Network:ConnectionRetriesCount", 0);

    public static int GetDelayBetweenConnectionRetries(this IConfiguration configuration)
    => configuration.GetValue("Network:DelayBetweenConnectionRetries", 0);

    private static TimeSpan GetTimeSpan(this IConfiguration configuration, string key, TimeSpan defaultValue)
    {
        var value = configuration.GetValue<string?>(key, null);

        if (value is null || !TimeSpan.TryParse(value, out var timeSpan))
        {
            return defaultValue;
        }

        return timeSpan;
    }

    private static T GetEnum<T>(this IConfiguration configuration, string key, T defaultValue) where T : struct, Enum
    {
        var value = configuration.GetValue<string?>(key, null);
        if (value is null || !Enum.TryParse(value, ignoreCase: true, out T result))
        {
            return defaultValue;
        }
        return result;
    }

    public static TimeSpan GetLeafsLifetime(this IConfiguration configuration)
    => configuration.GetTimeSpan("LeafsLifetime", Constants.LeafsLifetimeDefault);

    public static string? GetAzureVaultUrl(this IConfiguration configuration)
    => configuration.GetValue<string?>("AzureVaultUrl", null);

    public static KeySource GetKeySource(this IConfiguration configuration)
    => configuration.GetEnum("KeySource", KeySource.File);

    public static PassphraseSource GetPassphraseSource(this IConfiguration configuration)
    => configuration.GetEnum("PassphraseSource", PassphraseSource.Dashboard);

    public static string? GetOnionService(this IConfiguration configuration)
    => configuration.GetValue<string?>("OnionService", null);
}
