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

namespace Enigma5.App.Common;

public static class Constants
{
    public const int AuthTokenSize = 64;

    public static readonly TimeSpan VertexBroadcastMinimumPeriod = new(00, 05, 00);

    public static readonly TimeSpan LeafsLifetimeDefault = new(03, 00, 00, 00);

    public static readonly int MaxSharedFileSize = 100 * 1024 * 1024;

    public static readonly int MaxSharedDataSize = 1024 * 512;

    public const string XImpersonateServiceHeaderKey = "X-Impersonate-Service";

    public const string HubConnectionLocalIpKey = "HubConnectionLocalIp";

    public const string HubConnectionLocalPortKey = "HubConnectionLocalPort";

    public const string OnionRoutingEndpoint = "OnionRouting";

    public const string InfoEndpoint = "Info";

    public const string VerticesEndpoint = "Vertices";

    public const string ShareEndpoint = "Share";

    public const string VertexEndpoint = "Vertex";

    public const string LocalVertexEndpoint = "LocalVertex";

    public const string FileEndpoint = "File";

    public const string IncrementSharedDataAccessCountEndpoint = "IncrementSharedDataAccessCount";

    public const string IncrementFileAccessCountEndpoint = "IncrementFileAccessCount";

    public const string DashboardPageEndpoint = "Dashboard";

    public const string MessagesCleanupRecurringJob = "messages-cleanup";

    public const string SharedDataCleanupRecurringJob = "shared-data-cleanup";

    public const string FilesCleanupRecurringJob = "files-cleanup";

    public const string InvokeNetworkBridgeRecurringJob = "invoke-network-bridge";

    public const string MessagesCleanupJobInterval = "*/5 * * * *";

    public const string SharedDataCleanupJobInterval = "*/5 * * * *";

    public const string FilesCleanupJobInterval = "*/5 * * * *";

    public const string InvokeNetworkBridgeJobInterval = "*/10 * * * *";

    public const string NativeLibsRelativePathTemplate = "runtimes/{0}/native/{1}";

    public const string Libaenigma = "libaenigma.so";

    public static class Serilog
    {
        public const string HubMethodNameKey = "HubMethodName";

        public const string HubMethodArgumentsKey = "HubMethodArguments";

        public const string ConnectionVectorMethodNameKey = "ConnectionVectorMethodName";

        public const string HubConnectionsProxyMethodNameKey = "HubConnectionsProxyMethodName";

        public const string BridgeMethodNameKey = "BridgeMethodName";

        public const string ConnectionVectorKey = "ConnectionVector";
        
        public const string ConnectionIdKey = "ConnectionId";

        public const string DestinationConnectionIdKey = "DestinationConnectionId";

        public const string CommandKey = "Command";

        public const string CommandResultKey = "CommendResult";

        public const string AddressKey = "Address";
    }
}
