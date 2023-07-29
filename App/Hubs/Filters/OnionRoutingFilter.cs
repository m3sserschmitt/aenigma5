using Microsoft.AspNetCore.SignalR;

using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Contracts;
using Enigma5.App.Hubs.Sessions;

namespace Enigma5.App.Hubs.Filters;

public class OnionRoutingFilter : BaseFilter<IOnionParsingHub, OnionRoutingAttribute>
{
    private readonly SessionManager sessionManager;

    public OnionRoutingFilter(SessionManager sessionManager)
    {
        this.sessionManager = sessionManager;    
    }

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var onionParserHub = new OnionParsingHubAdapter(invocationContext.Hub);

        if(onionParserHub.Next != null)
        {
            var onionRouterHub = new OnionRoutingHubAdapter(invocationContext.Hub);

            if(sessionManager.TryGetConnectionId(onionParserHub.Next, out string? connectionId))
            {
                onionRouterHub.DestinationConnectionId = connectionId;
            }
        }

        return await next(invocationContext);
    }
}
