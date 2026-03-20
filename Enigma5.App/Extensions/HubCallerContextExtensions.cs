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

namespace Enigma5.App.Extensions;

public static class HubCallerContextExtensions
{
    public static void MapConnectionDetails(this HubCallerContext context)
    {
        var httpContext = context.GetHttpContext();
        context.Items[Common.Constants.XImpersonateServiceHeaderKey] = httpContext?.Request.Headers[Common.Constants.XImpersonateServiceHeaderKey].FirstOrDefault()?.ToString();
        context.Items[Common.Constants.HubConnectionLocalIpKey] = httpContext?.Connection.LocalIpAddress;
        context.Items[Common.Constants.HubConnectionLocalPortKey] = httpContext?.Connection.LocalPort;
    }
}
