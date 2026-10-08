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

using System.Net;
using Enigma5.App.Middlewares;
using Enigma5.Tests.Base;
using Microsoft.AspNetCore.Http;

namespace Enigma5.App.Tests.Middlewares;

public class HttpBlacklistAuthorizationMiddlewareTests
{
    private static async Task<(int Status, bool Reached)> Request(string path, int localPort)
    {
        var configuration = TestConfiguration.Create(
            ("HttpBlacklists:0:Endpoint", "http://127.0.0.1:8080"),
            ("HttpBlacklists:0:Items:0:Path", "/Dashboard"),
            ("HttpBlacklists:0:Items:0:Methods:0", "GET"));
        var reached = false;
        var middleware = new HttpBlacklistAuthorizationMiddleware(_ => { reached = true; return Task.CompletedTask; }, configuration, new CapturingLogger<HttpBlacklistAuthorizationMiddleware>());
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = path;
        context.Connection.LocalIpAddress = IPAddress.Loopback;
        context.Connection.LocalPort = localPort;

        await middleware.InvokeAsync(context);

        return (context.Response.StatusCode, reached);
    }

    [Fact]
    public async Task A_refused_request_is_answered_with_404_and_goes_no_further()
    {
        Assert.Equal((StatusCodes.Status404NotFound, false), await Request("/Dashboard", 8080));
    }

    [Fact]
    public async Task An_allowed_request_goes_on_to_the_next_step()
    {
        Assert.Equal((StatusCodes.Status200OK, true), await Request("/Info", 8080));
        Assert.Equal((StatusCodes.Status200OK, true), await Request("/Dashboard", 8081));
    }
}
