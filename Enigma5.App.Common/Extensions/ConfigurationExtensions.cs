/*
    Aenigma - Federated messaging system
    Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

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

using Enigma5.App.Common.Enums;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Common.Extensions;

public static class ConfigurationExtensions
{
    private static string? GetStringValue(this IConfiguration configuration, string key)
    {
        var value = configuration.GetValue<string?>(key, null);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static string? GetHttpEndpoint(this IConfiguration configuration)
    => configuration.GetStringValue("Kestrel:EndPoints:Http:Url")?.Trim('/', ' ');

    public static string? GetControlHttpEndpoint(this IConfiguration configuration)
    => configuration.GetStringValue("Kestrel:EndPoints:HttpControl:Url")?.Trim('/', ' ');

    public static string? GetHostname(this IConfiguration configuration)
    => configuration.GetStringValue("Hostname")?.Trim('/', ' ');

    public static string? GetDatabaseConnectionString(this IConfiguration configuration)
    => configuration.GetConnectionString("DbConnectionString");

    public static string? GetPublicEndpoint(this IConfiguration configuration)
    {
        var service = configuration.GetHostname();
        if(string.IsNullOrWhiteSpace(service))
        {
            service = configuration.GetOnionService();
        }
        return string.IsNullOrWhiteSpace(service) ? null : service;
    }

    public static string? GetSharedDataUrl(this IConfiguration configuration, string tag)
    {
        
        var service = configuration.GetPublicEndpoint();
        if(string.IsNullOrWhiteSpace(service))
        {
            return null;
        }

        return $"{service}/{Constants.ShareEndpoint}?Tag={tag}";
    }

    public static DateTimeOffset? GetSharedDataValidityDate(this IConfiguration configuration)
    => DateTimeOffset.UtcNow + configuration.GetSharedDataRetentionPeriod();

    public static string? GetFileUrl(this IConfiguration configuration, string tag)
    {
        var service = configuration.GetPublicEndpoint();
        if(string.IsNullOrWhiteSpace(service))
        {
            return null;
        }

        return $"{service}/{Constants.FileEndpoint}?Tag={tag}";
    }

    public static DateTimeOffset? GetFileValidityDate(this IConfiguration configuration)
    => DateTimeOffset.UtcNow + configuration.GetFilesRetentionPeriod();

    public static string? GetPrivateKeyPath(this IConfiguration configuration)
    => configuration.GetStringValue("PrivateKeyPath");

    public static string? GetPublicKeyPath(this IConfiguration configuration)
    => configuration.GetStringValue("PublicKeyPath");

    public static string? GetWebContentDirectory(this IConfiguration configuration)
    => configuration.GetStringValue("WebContentDirectory");

    public static string? GetWebContentFilePath(this IConfiguration configuration, string? tag)
    {
        var webContentDirectory = configuration.GetWebContentDirectory();
        var normalizedTag = tag.NormalizeTag();
        if (string.IsNullOrWhiteSpace(webContentDirectory) || normalizedTag is null)
        {
            return null;
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(webContentDirectory)) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root, normalizedTag));
        return fullPath.StartsWith(root, StringComparison.Ordinal) ? fullPath : null;
    }

    public static TimeSpan GetMessageRetentionPeriod(this IConfiguration configuration)
    => configuration.GetTimeSpan("MessageRetentionPeriod", Constants.DefaultMessageRetentionPeriod);

    public static TimeSpan GetSentMessageRetentionPeriod(this IConfiguration configuration)
    => configuration.GetTimeSpan("SentMessageRetentionPeriod", Constants.DefaultSentMessageRetentionPeriod);

    public static TimeSpan GetSharedDataRetentionPeriod(this IConfiguration configuration)
    => configuration.GetTimeSpan("SharedDataRetentionPeriod", Constants.DefaultSharedDataRetentionPeriod);

    public static TimeSpan GetFilesRetentionPeriod(this IConfiguration configuration)
    => configuration.GetTimeSpan("FilesRetentionPeriod", Constants.DefaultFilesRetentionPeriod);

    public static string? GetPassphraseKeyPath(this IConfiguration configuration)
    => configuration.GetStringValue("PassphrasePath");

    public static int GetDelayBetweenConnectionRetries(this IConfiguration configuration)
    => configuration.GetValue("Network:DelayBetweenConnectionRetries", Constants.DefaultDelayBetweenConnectionRetries);

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

    public static TimeSpan GetVertexLifetime(this IConfiguration configuration)
    => configuration.GetTimeSpan("VertexLifetime", Constants.DefaultVertexLifetime);

    public static TimeSpan GetUnlistedVertexGracePeriod(this IConfiguration configuration)
    => configuration.GetTimeSpan("UnlistedVertexGracePeriod", Constants.DefaultUnlistedVertexGracePeriod);

    public static string? GetAzureVaultUrl(this IConfiguration configuration)
    => configuration.GetStringValue("AzureVaultUrl");

    public static KeySource GetKeySource(this IConfiguration configuration)
    => configuration.GetEnum("KeySource", KeySource.File);

    public static PassphraseSource GetPassphraseSource(this IConfiguration configuration)
    => configuration.GetEnum("PassphraseSource", PassphraseSource.Dashboard);

    public static DbProvider GetDbProvider(this IConfiguration configuration)
    => configuration.GetEnum("DbProvider", DbProvider.Sqlite);

    public static string? GetOnionService(this IConfiguration configuration)
    => configuration.GetStringValue("OnionService");

    public static string? GetSocks5Proxy(this IConfiguration configuration)
    => configuration.GetStringValue("Socks5Proxy");

    public static long GetSharedFileMaxSize(this IConfiguration configuration)
    => configuration.GetValue("SharedFileMaxSize", Constants.DefaultSharedFileMaxSize);

    public static long GetSharedDataMaxSize(this IConfiguration configuration)
    => configuration.GetValue("SharedDataMaxSize", Constants.DefaultSharedDataMaxSize);

    public static PassphrasePersistence GetPassphrasePersistence(this IConfiguration configuration)
    => configuration.GetValue("PassphrasePersistence", PassphrasePersistence.Persistent);
}
