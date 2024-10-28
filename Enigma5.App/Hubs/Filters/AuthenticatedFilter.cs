/*
    Aenigma - Federal messaging system
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Hubs.Adapters;
using Enigma5.App.Hubs.Sessions;
using Enigma5.App.Models.HubInvocation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Enigma5.App.Hubs.Filters;

public class AuthenticatedFilter(
    SessionManager sessionManager,
    ILogger<AuthenticatedFilter> logger
    ) : BaseFilter<IIdentityHub, AuthenticatedAttribute>
{
    private readonly SessionManager _sessionManager = sessionManager;

    private readonly ILogger<AuthenticatedFilter> _logger = logger;

    protected override bool CheckArguments(HubInvocationContext invocationContext) => true;

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (_sessionManager.TryGetAddress(invocationContext.Context.ConnectionId, out string? address))
        {
            _logger.LogDebug($"ConnectionId {{{nameof(invocationContext.Context.ConnectionId)}}} resolved to address {{address}}.", invocationContext.Context.ConnectionId, address);
            _ = new IdentityHubAdapter(invocationContext.Hub)
            {
                ClientAddress = address
            };
            return await next(invocationContext);
        }

        _logger.LogDebug($"ConnectionId {{{nameof(invocationContext.Context.ConnectionId)}}} not authenticated thus it cannot be resolved to an address.", invocationContext.Context.ConnectionId);
        return EmptyErrorResult.Create(InvocationErrors.ONION_PARSING_FAILED);
    }
}
