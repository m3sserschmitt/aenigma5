using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs.Extensions;

public static class HubInvocationContextExtensions
{
    public static T? MethodInvocationArgument<T>(this HubInvocationContext invocationContext, int index)
    where T : class
    => invocationContext.HubMethodArguments[index] is T argument ? argument : null;
}
