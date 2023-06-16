using Enigma5.App.Hubs.Contracts;
using Enigma5.Message;
using Enigma5.App.Hubs.Extensions;

using Microsoft.AspNetCore.SignalR;

public class OnionRouterHubAdapter : IOnionRouterHub
{
    private readonly IOnionRouterHub? onionRouterHub;

    public OnionRouterHubAdapter(Hub hub)
    {
        onionRouterHub = hub.As<IOnionRouterHub>();
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
