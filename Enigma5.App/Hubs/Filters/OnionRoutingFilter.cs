using Microsoft.AspNetCore.SignalR;
using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Sessions;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Hubs.Adapters;
using Microsoft.Extensions.Logging;
using Enigma5.App.Models.HubInvocation;

namespace Enigma5.App.Hubs.Filters;

public class OnionRoutingFilter(SessionManager sessionManager, ILogger<OnionRoutingFilter> logger) : BaseFilter<IOnionParsingHub, OnionRoutingAttribute>
{
    private readonly SessionManager _sessionManager = sessionManager;

    private readonly ILogger<OnionRoutingFilter> _logger = logger;

    protected override bool CheckArguments(HubInvocationContext invocationContext) => true;

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
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
