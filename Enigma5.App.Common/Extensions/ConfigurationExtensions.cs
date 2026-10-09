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

using System.Globalization;
using Enigma5.App.Common.Enums;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Common.Extensions;

public static class ConfigurationExtensions
{
    private delegate bool Parser<T>(string value, out T result);

    private interface ITypedSetting
    {
        InvalidSetting? Check(IConfiguration configuration);
    }

    // A setting with a type other than text. Its value is read with Parse; a missing value, or one that
    // Parse rejects, is replaced by DefaultValue. Check reports a value that is present but rejected.
    private sealed record TypedSetting<T>(string Key, T DefaultValue, Parser<T> Parse) : ITypedSetting
    {
        public T Read(IConfiguration configuration)
        {
            var value = configuration.GetStringValue(Key);
            return value is not null && Parse(value, out var result) ? result : DefaultValue;
        }

        public InvalidSetting? Check(IConfiguration configuration)
        {
            var value = configuration.GetStringValue(Key);
            return value is not null && !Parse(value, out _) ? new(Key, value, $"{DefaultValue}") : null;
        }
    }

    private static bool TryParseTimeSpan(string value, out TimeSpan result)
    => TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out result);

    // Numbers are accepted only if they name a defined choice.
    private static bool TryParseEnum<T>(string value, out T result) where T : struct, Enum
    => Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(result);

    private static bool TryParseInt(string value, out int result)
    => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    private static bool TryParseLong(string value, out long result)
    => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    private static readonly TypedSetting<TimeSpan> MessageRetentionPeriod = new("MessageRetentionPeriod", Constants.DefaultMessageRetentionPeriod, TryParseTimeSpan);

    private static readonly TypedSetting<TimeSpan> SentMessageRetentionPeriod = new("SentMessageRetentionPeriod", Constants.DefaultSentMessageRetentionPeriod, TryParseTimeSpan);

    private static readonly TypedSetting<TimeSpan> SharedDataRetentionPeriod = new("SharedDataRetentionPeriod", Constants.DefaultSharedDataRetentionPeriod, TryParseTimeSpan);

    private static readonly TypedSetting<TimeSpan> FilesRetentionPeriod = new("FilesRetentionPeriod", Constants.DefaultFilesRetentionPeriod, TryParseTimeSpan);

    private static readonly TypedSetting<TimeSpan> VertexLifetime = new("VertexLifetime", Constants.DefaultVertexLifetime, TryParseTimeSpan);

    private static readonly TypedSetting<TimeSpan> UnlistedVertexGracePeriod = new("UnlistedVertexGracePeriod", Constants.DefaultUnlistedVertexGracePeriod, TryParseTimeSpan);

    private static readonly TypedSetting<DbProvider> DbProviderSetting = new("DbProvider", Constants.DefaultDbProvider, TryParseEnum);

    private static readonly TypedSetting<KeySource> KeySourceSetting = new("KeySource", Constants.DefaultKeySource, TryParseEnum);

    private static readonly TypedSetting<PassphraseSource> PassphraseSourceSetting = new("PassphraseSource", Constants.DefaultPassphraseSource, TryParseEnum);

    private static readonly TypedSetting<PassphrasePersistence> PassphrasePersistenceSetting = new("PassphrasePersistence", Constants.DefaultPassphrasePersistence, TryParseEnum);

    private static readonly TypedSetting<long> SharedDataMaxSize = new("SharedDataMaxSize", Constants.DefaultSharedDataMaxSize, TryParseLong);

    private static readonly TypedSetting<long> SharedFileMaxSize = new("SharedFileMaxSize", Constants.DefaultSharedFileMaxSize, TryParseLong);

    private static readonly TypedSetting<int> DelayBetweenConnectionRetries = new("Network:DelayBetweenConnectionRetries", Constants.DefaultDelayBetweenConnectionRetries, TryParseInt);

    // Every setting with a type other than text. A new one must be added here to be checked at startup.
    private static readonly ITypedSetting[] TypedSettings =
    [
        MessageRetentionPeriod, SentMessageRetentionPeriod, SharedDataRetentionPeriod, FilesRetentionPeriod,
        VertexLifetime, UnlistedVertexGracePeriod,
        DbProviderSetting, KeySourceSetting, PassphraseSourceSetting, PassphrasePersistenceSetting,
        SharedDataMaxSize, SharedFileMaxSize, DelayBetweenConnectionRetries
    ];

    // Settings whose value is present but cannot be read. The node uses their defaults instead.
    public static List<InvalidSetting> GetInvalidSettings(this IConfiguration configuration)
    => [.. TypedSettings.Select(setting => setting.Check(configuration)).OfType<InvalidSetting>()];

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
    => configuration.GetStringValue("ConnectionStrings:DbConnectionString") ?? Constants.DefaultDbConnectionString;

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
    => configuration.GetStringValue("PrivateKeyPath") ?? Constants.DefaultPrivateKeyPath;

    public static string? GetPublicKeyPath(this IConfiguration configuration)
    => configuration.GetStringValue("PublicKeyPath") ?? Constants.DefaultPublicKeyPath;

    public static string? GetWebContentDirectory(this IConfiguration configuration)
    => configuration.GetStringValue("WebContentDirectory") ?? Constants.DefaultWebContentDirectory;

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
    => MessageRetentionPeriod.Read(configuration);

    public static TimeSpan GetSentMessageRetentionPeriod(this IConfiguration configuration)
    => SentMessageRetentionPeriod.Read(configuration);

    public static TimeSpan GetSharedDataRetentionPeriod(this IConfiguration configuration)
    => SharedDataRetentionPeriod.Read(configuration);

    public static TimeSpan GetFilesRetentionPeriod(this IConfiguration configuration)
    => FilesRetentionPeriod.Read(configuration);

    public static string? GetPassphraseKeyPath(this IConfiguration configuration)
    => configuration.GetStringValue("PassphrasePath");

    public static int GetDelayBetweenConnectionRetries(this IConfiguration configuration)
    => DelayBetweenConnectionRetries.Read(configuration);

    public static TimeSpan GetVertexLifetime(this IConfiguration configuration)
    => VertexLifetime.Read(configuration);

    public static TimeSpan GetUnlistedVertexGracePeriod(this IConfiguration configuration)
    => UnlistedVertexGracePeriod.Read(configuration);

    public static string? GetAzureVaultUrl(this IConfiguration configuration)
    => configuration.GetStringValue("AzureVaultUrl");

    public static KeySource GetKeySource(this IConfiguration configuration)
    => KeySourceSetting.Read(configuration);

    public static PassphraseSource GetPassphraseSource(this IConfiguration configuration)
    => PassphraseSourceSetting.Read(configuration);

    public static DbProvider GetDbProvider(this IConfiguration configuration)
    => DbProviderSetting.Read(configuration);

    public static string? GetOnionService(this IConfiguration configuration)
    => configuration.GetStringValue("OnionService");

    public static string? GetSocks5Proxy(this IConfiguration configuration)
    => configuration.GetStringValue("Socks5Proxy") ?? Constants.DefaultSocks5Proxy;

    public static long GetSharedFileMaxSize(this IConfiguration configuration)
    => SharedFileMaxSize.Read(configuration);

    public static long GetSharedDataMaxSize(this IConfiguration configuration)
    => SharedDataMaxSize.Read(configuration);

    public static PassphrasePersistence GetPassphrasePersistence(this IConfiguration configuration)
    => PassphrasePersistenceSetting.Read(configuration);
}
