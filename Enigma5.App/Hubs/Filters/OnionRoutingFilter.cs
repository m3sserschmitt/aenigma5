using Microsoft.AspNetCore.SignalR;
using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Sessions;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Hubs.Adapters;

namespace Enigma5.App.Hubs.Filters;

public class OnionRoutingFilter(SessionManager sessionManager) : BaseFilter<IOnionParsingHub, OnionRoutingAttribute>
{
    private readonly SessionManager _sessionManager = sessionManager;

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
            }
        }

        return await next(invocationContext);
    }
}
