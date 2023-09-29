using System.Reflection;
using Enigma5.App.Attributes;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs;

public abstract class RoutingHubBase<TSelf> : Hub
where TSelf : RoutingHubBase<TSelf>
{
    protected async Task RespondAsync(string method, object? arg1)
    => await Clients.Client(Context.ConnectionId).SendAsync(method, arg1);

    protected async Task RouteMessage(string destinationConnectionId, byte[] data)
    {
        var routingMethod = typeof(TSelf).GetMethods()
                .Where(m => m.GetCustomAttribute<OnionRoutingAttribute>() != null)
                .SingleOrDefault()
                ?? throw new Exception($"Type {nameof(TSelf)} should contain exactly one method with {nameof(OnionRoutingAttribute)}.");

        await Clients.Client(destinationConnectionId).SendAsync(routingMethod.Name, Convert.ToBase64String(data));
    }
}
