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
    public static readonly TimeSpan VertexBroadcastMinimumPeriod = new(01, 00, 00);

    public static readonly TimeSpan LeafsLifetimeDefault = new(03, 00, 00, 00);
    public static readonly int MaxSharedFileSize = 100 * 1024 * 1024;

    public static readonly int MaxSharedDataSize = 1 * 1024 * 1024;

    public const string OnionRoutingEndpoint = "OnionRouting";

    public const string InfoEndpoint = "Info";

    public const string VerticesEndpoint = "Vertices";

    public const string ShareEndpoint = "Share";

    public const string VertexEndpoint = "Vertex";

    public const string FileEndpoint = "File";

    public const string IncrementSharedDataAccessCountEndpoint = "IncrementSharedDataAccessCount";

    public const string IncrementFileAccessCountEndpoint = "IncrementFileAccessCount";

    public const string ProductionConfigurationFileName = "/usr/local/etc/aenigma/appsettings.Production.json";
}