using Microsoft.AspNetCore.SignalR;

using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Contracts;
using Enigma5.App.Contracts;

namespace Enigma5.App.Hubs.Filters;

public class OnionRoutingFilter : BaseFilter<IOnionParsingHub, OnionRoutingAttribute>
{
    private readonly IConnectionsMapper connectionsMapper;

    public OnionRoutingFilter(IConnectionsMapper connectionsMapper)
    {
        this.connectionsMapper = connectionsMapper;    
    }

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var onionParserHub = new OnionParsingHubAdapter(invocationContext.Hub);

        if(onionParserHub.Next != null)
        {
            var onionRouterHub = new OnionRoutingHubAdapter(invocationContext.Hub);

            if(connectionsMapper.TryGetConnectionId(onionParserHub.Next, out string? connectionId))
            {
                onionRouterHub.DestinationConnectionId = connectionId;
            }
        }

        return await next(invocationContext);
    }
}
