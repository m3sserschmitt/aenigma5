using Microsoft.AspNetCore.SignalR;

using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Contracts;
using Enigma5.App.Contracts;
using Enigma5.Crypto;

namespace Enigma5.App.Hubs.Filters;

public class OnionRouterFilter : BaseFilter<IOnionParserHub, OnionRouterAttribute>
{
    private readonly IConnectionsMapper connectionsMapper;

    public OnionRouterFilter(IConnectionsMapper connectionsMapper)
    {
        this.connectionsMapper = connectionsMapper;    
    }

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var onionParserHub = new OnionParserHubAdapter(invocationContext.Hub);

        if(onionParserHub.Next != null)
        {
            var onionRouterHub = new OnionRouterHubAdapter(invocationContext.Hub);

            if(connectionsMapper.TryGetConnectionId(onionParserHub.Next, out string? connectionId))
            {
                onionRouterHub.DestinationConnectionId = connectionId;
            }
        }

        return await next(invocationContext);
    }
}
