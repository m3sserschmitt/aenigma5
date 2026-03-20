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

using Microsoft.AspNetCore.SignalR;
using Enigma5.App.Common.Extensions;
using Enigma5.App.Models;
using System.Net;

namespace Enigma5.App.Extensions;

public static class ConfigurationExtensions
{
    public static bool IsHttpRequestCallAuthorized(this IConfiguration configuration, HttpContext httpContext, ILogger? logger = null)
    {
        var blacklists = configuration.GetHttpBlacklists();
        if (blacklists.Count == 0)
        {
            return true;
        }

        var requestMethod = httpContext.Request.Method;

        if (string.IsNullOrWhiteSpace(requestMethod))
        {
            logger?.LogError($"HttpContext request method resolved to null.");
            return false;
        }

        var requestLocalIp = httpContext.Connection.LocalIpAddress;
        if (requestLocalIp == null)
        {
            logger?.LogError($"HttpContext connection IP resolved to null.");
            return false;
        }

        var matchingBlacklist = blacklists.FirstOrDefault(
            item => item.Endpoint.MatchUrl(requestLocalIp, httpContext.Connection.LocalPort)
        );

        if (matchingBlacklist == null)
        {
            return true;
        }

        var requestPath = httpContext.Request.Path;
        return !(matchingBlacklist.Items?.Any(
            item => requestPath.StartsWithSegments(item.Path)
            && item.Methods.Any(method => string.Equals(method, requestMethod, StringComparison.OrdinalIgnoreCase))
        ) ?? false);
    }

    public static bool IsHubMethodCallAuthorized(this IConfiguration configuration, HubInvocationContext hubInvocationContext, ILogger? logger = null)
    {
        var blacklists = configuration.GetHubBlacklists();
        if (blacklists.Count == 0)
        {
            return false;
        }
        
        var httpContext = hubInvocationContext.Context.GetHttpContext();
        var requestLocalPort = (httpContext?.Connection.LocalPort ?? hubInvocationContext.Context.Items[Common.Constants.HubConnectionLocalPortKey]) as int?;

        if (!requestLocalPort.HasValue)
        {
            logger?.LogError($"HttpContext connection port resolved to null.");
            return false;
        }

        if ((httpContext?.Connection.LocalIpAddress ?? hubInvocationContext.Context.Items[Common.Constants.HubConnectionLocalIpKey]) is not IPAddress requestLocalIp)
        {
            logger?.LogError($"HttpContext connection IP resolved to null.");
            return false;
        }

        var matchingBlacklist = blacklists.FirstOrDefault(
            item => item.Endpoint.MatchUrl(requestLocalIp, requestLocalPort.Value)
        );

        if (matchingBlacklist == null)
        {
            return true;
        }

        return !(matchingBlacklist.Items?.Any(
            item => item.Methods?.Any(method => string.Equals(method, hubInvocationContext.HubMethodName, StringComparison.OrdinalIgnoreCase)) ?? false
        ) ?? false);
    }

    public static List<HttpBlacklistDto> GetHttpBlacklists(this IConfiguration configuration)
    => configuration.GetSection("HttpBlacklists").Get<List<HttpBlacklistDto>>() ?? [];

    public static List<HubBlacklistDto> GetHubBlacklists(this IConfiguration configuration)
    => configuration.GetSection("HubBlacklists").Get<List<HubBlacklistDto>>() ?? [];
}
