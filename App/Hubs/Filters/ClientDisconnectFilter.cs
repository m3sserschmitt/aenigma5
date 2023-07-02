using Enigma5.App.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs.Filters;

public class ClientDisconnectFilter : IHubFilter
{
    private readonly IConnectionsMapper connectionsMapper;

    public ClientDisconnectFilter(IConnectionsMapper connectionsMapper)
    {
        this.connectionsMapper = connectionsMapper;
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        connectionsMapper.Remove(context.Hub.Context.ConnectionId);

        await context.Hub.OnDisconnectedAsync(exception);
        await next(context, exception);
    }
}
