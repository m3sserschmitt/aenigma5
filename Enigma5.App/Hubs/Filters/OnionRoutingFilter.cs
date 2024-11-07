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

using Microsoft.AspNetCore.SignalR;
using Enigma5.App.Attributes;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Hubs.Adapters;
using Microsoft.Extensions.Logging;
using Enigma5.App.Models.HubInvocation;
using Enigma5.App.Hubs.Sessions.Contracts;

namespace Enigma5.App.Hubs.Filters;

public class OnionRoutingFilter(ISessionManager sessionManager, ILogger<OnionRoutingFilter> logger) : BaseFilter<IOnionParsingHub, OnionRoutingAttribute>
{
    private readonly ISessionManager _sessionManager = sessionManager;

    private readonly ILogger<OnionRoutingFilter> _logger = logger;

    protected override bool CheckArguments(HubInvocationContext invocationContext) => true;

    public override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var onionParserHub = new OnionParsingHubAdapter(invocationContext.Hub);

        if(onionParserHub.Next != null)
        {
            var onionRouterHub = new OnionRoutingHubAdapter(invocationContext.Hub);

            if(_sessionManager.TryGetConnectionId(onionParserHub.Next, out string? connectionId))
            {
                onionRouterHub.DestinationConnectionId = connectionId;
                _logger.LogDebug(
                    $"{{{nameof(onionParserHub.Next)}}} address resolved to connectionId {{{nameof(onionRouterHub.DestinationConnectionId)}}} for connectionId {{{nameof(invocationContext.Context.ConnectionId)}}}.",
                    onionParserHub.Next,
                    onionRouterHub.DestinationConnectionId,
                    invocationContext.Context.ConnectionId);
                return await next(invocationContext);
            }

            _logger.LogDebug($"ConnectionId not found for next address {{{nameof(onionParserHub.Next)}}}", onionParserHub.Next);
            return await next(invocationContext);
        }

        _logger.LogDebug($"Onion null next address for connectionId {{{nameof(invocationContext.Context.ConnectionId)}}}.", invocationContext.Context.ConnectionId);
        return EmptyErrorResult.Create(InvocationErrors.ONION_ROUTING_FAILED);
    }
}
