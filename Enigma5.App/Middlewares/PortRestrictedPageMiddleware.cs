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

using Enigma5.App.Common.Extensions;

namespace Enigma5.App.Middlewares;

public sealed class AuthorizedPortRestrictedPageMiddleware(RequestDelegate next, IConfiguration configuration, string path)
{
    private readonly RequestDelegate _next = next;

    private readonly IConfiguration _configuration = configuration;

    private readonly PathString _path = new(path);

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(_path))
        {
            if (!_configuration.IsAuthorizedHttpInvocation(context))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
        }
        await _next(context);
    }
}
