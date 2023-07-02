using Enigma5.App.Hubs.Contracts;
using Enigma5.App.Hubs.Extensions;

using Microsoft.AspNetCore.SignalR;

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
