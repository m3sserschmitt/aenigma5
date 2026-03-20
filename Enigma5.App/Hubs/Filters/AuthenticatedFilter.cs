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
using Enigma5.App.Hubs.Adapters;
using Enigma5.App.Hubs.Sessions.Contracts;
using Enigma5.App.Models.Contracts.Hubs;
using Enigma5.App.Models.HubInvocation;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs.Filters;

public class AuthenticatedFilter(
    ISessionManager sessionManager,
    ILogger<AuthenticatedFilter> logger
    ) : BaseFilter<IIdentityHub, AuthenticatedAttribute>
{
    private readonly ISessionManager _sessionManager = sessionManager;

    private readonly ILogger<AuthenticatedFilter> _logger = logger;

    protected override bool CheckArguments(HubInvocationContext invocationContext) => true;

    public override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var address = await _sessionManager.TryGetAddressAsync(invocationContext.Context.ConnectionId);
        if (address != null)
        {
            _logger.LogDebug($"ConnectionId {{{Common.Constants.Serilog.ConnectionIdKey}}} resolved to address {{{Common.Constants.Serilog.AddressKey}}}.", invocationContext.Context.ConnectionId, address);
            _ = new IdentityHubAdapter(invocationContext.Hub)
            {
                ClientAddress = address
            };
            return await next(invocationContext);
        }

        _logger.LogDebug($"ConnectionId {{{Common.Constants.Serilog.ConnectionIdKey}}} not authenticated thus it cannot be resolved to an address.", invocationContext.Context.ConnectionId);
        return EmptyErrorResultDto.Create(InvocationErrors.AUTHENTICATION_REQUIRED);
    }
}
