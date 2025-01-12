/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace NetworkBridge;

public static class HubConnectionExtensions
{
    private static Task InvokeAsync<T>(HubConnection target, string method, T data)
    {
        try
        {
            return target.InvokeAsync(method, data);
        }
        catch (Exception)
        {
            // TODO: Log this exception!
            return Task.CompletedTask;
        }
    }

    public static void Forward<T>(this ConnectionVector connectionVector, string method)
    where T : class
    {
        connectionVector.SourceOn<T>(method, async data => await connectionVector.InvokeTargetAsync(method, data, CancellationToken.None));
        connectionVector.TargetOn<T>(method, async data => await connectionVector.InvokeSourceAsync(method, data, CancellationToken.None));
    }

    public static void ForwardMessageRouting(this ConnectionVector connection)
    {
        connection.Forward<RoutingRequest>(nameof(IEnigmaHub.RouteMessage));
    }

    public static void ForwardBroadcasts(this ConnectionVector connection)
    {
        connection.Forward<VertexBroadcastRequest>(nameof(IEnigmaHub.Broadcast));
    }

    public static void ForwardCloseSignal(this ConnectionVector connection)
    {
        connection.SourceClosed += async _ => { try { await connection.StopTargetAsync(CancellationToken.None); } catch (Exception) { /* TODO: log exception */ } };
        connection.TargetClosed += async _ => { try { await connection.StopSourceAsync(CancellationToken.None); } catch (Exception) { /* TODO: log exception */ } };
    }

    public static async Task<bool> StartAsync(this IEnumerable<ConnectionVector> connections, CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(connections.Select(async connection => await connection.StartAsync(cancellationToken)));
        return results.All(result => result);
    }

    public static async Task<bool> StopAsync(this IEnumerable<ConnectionVector> connections, CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(connections.Select(async connection => await connection.StopAsync(cancellationToken)));
        return results.All(result => result);
    }

    public static async Task<bool> StartAuthenticationAsync(this IEnumerable<ConnectionVector> connections, CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(connections.Select(async connection => await connection.StartAuthenticationAsync(cancellationToken)));
        return results.All(result => result);
    }
}
