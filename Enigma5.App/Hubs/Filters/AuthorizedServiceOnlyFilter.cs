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

using Enigma5.App.Attributes;
using Microsoft.AspNetCore.SignalR;
using Enigma5.App.Common.Extensions;
using Enigma5.App.Models.HubInvocation;
using Enigma5.App.Models.Contracts.Hubs;

namespace Enigma5.App.Hubs.Filters;

public class AuthorizedServiceOnlyFilter(IConfiguration configuration, ILogger<AuthorizedServiceOnlyFilter> logger) : BaseFilter<IEnigmaHub, AuthorizedServiceOnlyAttribute>
{
    private readonly IConfiguration _configuration = configuration;

    private readonly ILogger<AuthorizedServiceOnlyFilter> _logger = logger;

    protected override bool CheckArguments(HubInvocationContext invocationContext) => true;

    public override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var httpContext = invocationContext.Context.GetHttpContext();
        if (httpContext == null)
        {
            _logger.LogError($"Invocation context has null HttpContext.");
            return false;
        }

        if (_configuration.IsAuthorizedHttpInvocation(httpContext))
        {
            _logger.LogDebug($"Connection {{{nameof(invocationContext.Context.ConnectionId)}}} authorized for {{{nameof(invocationContext.HubMethodName)}}} invocation.",
            invocationContext.Context.ConnectionId, invocationContext.HubMethodName);
            return await next(invocationContext);
        }
        _logger.LogDebug($"Connection {{{nameof(invocationContext.Context.ConnectionId)}}} not authorized for {{{nameof(invocationContext.HubMethodName)}}} invocation.",
        invocationContext.Context.ConnectionId, invocationContext.HubMethodName);
        return EmptyErrorResultDto.Create(InvocationErrors.INTERNAL_ERROR);
    }
}
