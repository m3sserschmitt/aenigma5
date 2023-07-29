using Enigma5.App.Hubs.Sessions;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs.Filters;

public class ClientDisconnectFilter : IHubFilter
{
    private readonly SessionManager sessionManager;

    public ClientDisconnectFilter(SessionManager sessionManager)
    {
        this.sessionManager = sessionManager;
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        sessionManager.Remove(context.Hub.Context.ConnectionId);

        await context.Hub.OnDisconnectedAsync(exception);
        await next(context, exception);
    }
}
