using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Hubs.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs.Adapters;

public class OnionRoutingHubAdapter : IOnionRoutingHub
{
    private readonly IOnionRoutingHub? onionRouterHub;

    public OnionRoutingHubAdapter(Hub hub)
    {
        onionRouterHub = hub.As<IOnionRoutingHub>();
    }

    public string? DestinationConnectionId
    {
        get => onionRouterHub?.DestinationConnectionId;
        set
        {
            if(onionRouterHub != null)
            {
                onionRouterHub.DestinationConnectionId = value;
            }
        }
    }
}
