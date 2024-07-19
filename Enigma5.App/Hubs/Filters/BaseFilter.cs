using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs.Filters;

public abstract class BaseFilter<THub, TMarker> : IHubFilter
where THub : class
where TMarker : Attribute
{
    protected virtual bool CheckMarker(HubInvocationContext invocationContext) =>
    Attribute.GetCustomAttribute(invocationContext.HubMethod, typeof(TMarker)) != null;

    protected abstract bool CheckArguments(HubInvocationContext invocationContext);

    protected virtual bool CheckHubType(HubInvocationContext invocationContext) => invocationContext.Hub is THub;

    protected abstract ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next);

    protected bool Check(HubInvocationContext invocationContext) =>
    CheckMarker(invocationContext) && CheckHubType(invocationContext) && CheckArguments(invocationContext);
    
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next) =>
    Check(invocationContext) ? await Handle(invocationContext, next) : await next(invocationContext);
}
