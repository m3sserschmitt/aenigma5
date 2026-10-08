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

namespace Enigma5.App.Common;

public static class Constants
{
    public const int AuthTokenSize = 64;

    public static readonly TimeSpan DefaultVertexBroadcastMinimumPeriod = TimeSpan.FromMinutes(06);

    public static readonly TimeSpan DefaultVertexLifetime = TimeSpan.FromMinutes(30);

    // How long a vertex that no other vertex lists is kept before a cleanup may remove it.
    public static readonly TimeSpan DefaultUnlistedVertexGracePeriod = TimeSpan.FromMinutes(6);

    public static readonly TimeSpan DefaultMessageRetentionPeriod = TimeSpan.FromDays(14);

    public static readonly TimeSpan DefaultSentMessageRetentionPeriod = TimeSpan.Zero;

    public static readonly TimeSpan DefaultSharedDataRetentionPeriod = TimeSpan.FromDays(14);

    public static readonly TimeSpan DefaultFilesRetentionPeriod = TimeSpan.FromDays(3);

    // Milliseconds.
    public const int DefaultDelayBetweenConnectionRetries = 3000;

    // Defaults of the settings, used when a setting is missing or its value cannot be read.
    // They are the same as the values in Enigma5.App/appsettings.json.
    public const DbProvider DefaultDbProvider = DbProvider.Sqlite;

    public const string DefaultDbConnectionString = "data source=aenigmaDb.sqlite";

    public const KeySource DefaultKeySource = KeySource.File;

    public const PassphraseSource DefaultPassphraseSource = PassphraseSource.Dashboard;

    public const PassphrasePersistence DefaultPassphrasePersistence = PassphrasePersistence.Persistent;

    public const string DefaultPrivateKeyPath = "private-key.pem";

    public const string DefaultPublicKeyPath = "public-key.pem";

    public const string DefaultWebContentDirectory = "./";

    public const string DefaultSocks5Proxy = "socks5://127.0.0.1:9050";

    public static readonly TimeSpan SignalRHandshakeTimeout = TimeSpan.FromSeconds(15);

    public static readonly TimeSpan SignalRClientTimeoutInterval = TimeSpan.FromSeconds(90);

    public static readonly TimeSpan SignalRServerTimeoutInterval = TimeSpan.FromSeconds(90);

    public static readonly TimeSpan SignalRKeepAliveInterval = TimeSpan.FromSeconds(20);

    public static readonly TimeSpan[] SignalRReconnectDelays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8),
        TimeSpan.FromSeconds(16),
    ];

    public const long SignalRStatefulReconnectBufferSize = 1 * 1024 * 1024;

    public const long DefaultSharedFileMaxSize = 64 * 1024 * 1024;

    public const long DefaultSharedDataMaxSize = 16 * 1024;

    public const int MessagesPageSize = 20;

    // Maximum number of messages returned by the deprecated hub method Pull.
    public const int LegacyPullMaxMessages = 128;

    // Maximum length of one base64 onion accepted by RouteMessage. Sized for 6 relay nodes plus the
    // recipient's layer (4096-bit keys) around a signed 256-character client message (~12 900 chars).
    public const int MaxOnionSize = 16 * 1024;

    // RSA key sizes, in bits, that are accepted for a public key received from a caller.
    public const int MinPublicKeySize = 2048;

    public const int MaxPublicKeySize = 8192;

    // Longest text of a public key in PEM form, in characters; a key of the largest size needs about 1500.
    public const int MaxPublicKeyLength = 4096;

    // Largest incoming hub message: a full batch of maximum-size onions plus room for JSON framing.
    public const long SignalRMaximumReceiveMessageSize = MessagesPageSize * MaxOnionSize + 4 * 1024;

    public const string XImpersonateServiceHeaderKey = "X-Impersonate-Service";

    public const string HubConnectionLocalIpKey = "HubConnectionLocalIp";

    public const string HubConnectionLocalPortKey = "HubConnectionLocalPort";

    // Highest pending message id returned by Pull or Pull2 on a connection; limits what Cleanup confirms.
    public const string HubConnectionLastPulledMessageIdKey = "HubConnectionLastPulledMessageId";

    public const string OnionRoutingEndpoint = "OnionRouting";

    public const string OpenApiName = "Aenigma API";

    public const string OpenApiEndpoint = "/openapi/v1.json";

    public const string RootEndpoint = "/";

    public const string InfoEndpoint = "Info";

    public const string VerticesEndpoint = "Vertices";

    public const string ShareEndpoint = "Share";

    public const string VertexEndpoint = "Vertex";

    public const string LocalVertexEndpoint = "LocalVertex";

    public const string FileEndpoint = "File";

    public const string IncrementSharedDataAccessCountEndpoint = "IncrementSharedDataAccessCount";

    public const string IncrementFileAccessCountEndpoint = "IncrementFileAccessCount";

    public const string DashboardPageEndpoint = "Dashboard";

    public const string JobsDashboardEndpoint = "/Jobs";

    public const string MessagesCleanupRecurringJob = "messages-cleanup";

    public const string SharedDataCleanupRecurringJob = "shared-data-cleanup";

    public const string FilesCleanupRecurringJob = "files-cleanup";

    public const string GraphCleanupRecurringJob = "graph-cleanup";

    public const string InvokeNetworkBridgeRecurringJob = "invoke-network-bridge";

    public const string MessagesCleanupJobInterval = "*/5 * * * *";

    public const string SharedDataCleanupJobInterval = "*/5 * * * *";

    public const string FilesCleanupJobInterval = "*/5 * * * *";

    public const string GraphCleanupJobInterval = "*/5 * * * *";

    public const string InvokeNetworkBridgeJobInterval = "*/5 * * * *";

    public const string NativeLibsRelativePathTemplate = "runtimes/{0}/native/{1}";

    public const string Libaenigma = "libaenigma.so";

    public static class Serilog
    {
        public const string HubMethodNameKey = "HubMethodName";

        public const string HubMethodInvocationErrorsKey = "HubMethodInvocationErrors";

        public const string HubMethodArgumentsKey = "HubMethodArguments";

        public const string ConnectionVectorMethodNameKey = "ConnectionVectorMethodName";

        public const string HubConnectionsProxyMethodNameKey = "HubConnectionsProxyMethodName";

        public const string BridgeMethodNameKey = "BridgeMethodName";

        public const string ConnectionVectorKey = "ConnectionVector";

        public const string ConnectionIdKey = "ConnectionId";

        public const string DestinationConnectionIdKey = "DestinationConnectionId";

        public const string CommandKey = "Command";

        public const string CommandResultKey = "CommandResult";

        public const string AddressKey = "Address";
    }
}
