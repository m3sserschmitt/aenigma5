/*
    Aenigma - Federated messaging system
    Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

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

using System.Collections.Concurrent;
using Enigma5.App.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Enigma5.App.Tests.Hubs;

// The hub as one connection sees it, without a web server and without the filters. What the filters set on
// the hub before a method runs (the address of the caller, the parsed onion) is set by the test.
internal sealed class HubConnection : IAsyncDisposable
{
    private readonly AsyncServiceScope _scope;

    public RoutingHub Hub { get; }

    public string ConnectionId { get; }

    public DefaultHttpContext HttpContext { get; } = new();

    // What the hub has sent to any connection: the receiving connection, the method and its argument.
    public ConcurrentQueue<(string ConnectionId, string Method, object? Argument)> Sent { get; } = new();

    public HubConnection(TestNode node, string connectionId)
    {
        ConnectionId = connectionId;
        _scope = node.CreateScope();

        var features = new FeatureCollection();
        var httpContextFeature = Substitute.For<IHttpContextFeature>();
        httpContextFeature.HttpContext.Returns(HttpContext);
        features.Set(httpContextFeature);
        var caller = Substitute.For<HubCallerContext>();
        caller.ConnectionId.Returns(connectionId);
        caller.Items.Returns(new Dictionary<object, object?>());
        caller.Features.Returns(features);

        var clients = Substitute.For<IHubCallerClients>();
        clients.Client(Arg.Any<string>()).Returns(call =>
        {
            var receiver = call.Arg<string>();
            var proxy = Substitute.For<ISingleClientProxy>();
            proxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>()).Returns(sending =>
            {
                Sent.Enqueue((receiver, sending.ArgAt<string>(0), sending.ArgAt<object?[]>(1).SingleOrDefault()));
                return Task.CompletedTask;
            });
            return proxy;
        });

        Hub = ActivatorUtilities.CreateInstance<RoutingHub>(_scope.ServiceProvider);
        Hub.Context = caller;
        Hub.Clients = clients;
    }

    // The hub sends without waiting, so a test waits for what it expects.
    public async Task<(string ConnectionId, string Method, object? Argument)> NextSent()
    {
        var limit = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < limit)
        {
            if (Sent.TryDequeue(out var sent))
            {
                return sent;
            }
            await Task.Delay(10);
        }
        throw new TimeoutException("The hub did not send anything.");
    }

    public async ValueTask DisposeAsync()
    {
        Hub.Dispose();
        await _scope.DisposeAsync();
    }
}
