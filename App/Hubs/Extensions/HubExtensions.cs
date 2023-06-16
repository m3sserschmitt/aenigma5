using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs.Extensions;

public static class HubExtensions
{
    public static T? As<T>(this Hub hub)
    where T : class
    => hub is T t ? t : null;
}
