using System.Reflection;
using Enigma5.App.Attributes;
using Enigma5.App.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs;

public abstract class RoutingHubBase<TSelf> : Hub
where TSelf : RoutingHubBase<TSelf>
{
    private readonly IConnectionsMapper connectionsMapper;

    protected RoutingHubBase(IConnectionsMapper connectionsMapper)
    {
        this.connectionsMapper = connectionsMapper;
    }

    protected async Task RespondAsync(string method, object? arg1)
    => await Clients.Client(Context.ConnectionId).SendAsync(method, arg1);

    protected async Task SendAsync(string connectionId, byte[] data)
    {
        try
        {
            var routingMethod = typeof(TSelf).GetMethods()
                .Where(m => m.GetCustomAttribute<OnionRoutingAttribute>() != null)
                .SingleOrDefault();

            if(routingMethod == null)
            {
                throw new Exception();
            }

            await Clients.Client(connectionId).SendAsync(routingMethod.Name, Convert.ToBase64String(data));

        } catch (Exception)
        {
            throw new Exception($"Type {nameof(TSelf)} should contain exactly one method with {nameof(OnionRoutingAttribute)}.");
        }
    }
}
