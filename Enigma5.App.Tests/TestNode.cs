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

using Enigma5.App.Data;
using Enigma5.App.Extensions;
using Enigma5.Security.Contracts;
using Enigma5.Tests.Base;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Enigma5.App.Tests;

// The services of a node, registered as the node itself registers them, with a database file and an upload
// folder of its own. Nothing listens on the network. The node's key is one of the fixed test keys.
internal sealed class TestNode : IAsyncDisposable
{
    private readonly TempFolder _folder = new();

    private readonly ServiceProvider _services;

    public FixedKeyCertificateManager Key { get; }

    public IConfiguration Configuration { get; }

    public string UploadFolder { get; }

    private TestNode(FixedKeyCertificateManager key, (string Key, string? Value)[] settings)
    {
        Key = key;
        UploadFolder = Directory.CreateDirectory(_folder.File("uploads")).FullName;
        Configuration = TestConfiguration.Create([
            // Without a pool every connection is closed for good when it is disposed, so the folder can be deleted
            // without clearing the pools, which would also close the connections of tests that run at the same time.
            ("ConnectionStrings:DbConnectionString", $"data source={_folder.File("node.sqlite")};Pooling=False"),
            ("WebContentDirectory", UploadFolder),
            ("Hostname", "http://node.example"),
            .. settings]);

        var services = new ServiceCollection();
        services.AddSingleton(Configuration);
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        new StartupConfiguration(Configuration).ConfigureServices(services);
        services.AddSingleton<ICertificateManager>(key);
        _services = services.BuildServiceProvider();
        _services.MigrateDatabase();
    }

    public static async Task<TestNode> StartAsync(params (string Key, string? Value)[] settings)
    => await StartAsync(FixedKeyCertificateManager.Key1(), settings);

    public static async Task<TestNode> StartAsync(FixedKeyCertificateManager key, params (string Key, string? Value)[] settings)
    {
        var node = new TestNode(key, settings);
        await node.Get<NetworkGraph>().GenerateLocalVertexAsync();
        return node;
    }

    public T Get<T>() where T : notnull => _services.GetRequiredService<T>();

    // Sends a command or query the way a request does: with services of its own scope.
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }

    // Calls an endpoint method the way a request does: with a mediator of its own scope.
    public async Task<T> Request<T>(Func<IMediator, Task<T>> call)
    {
        await using var scope = _services.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<IMediator>());
    }

    // Reads from, or prepares, the database with a context of its own.
    public async Task<T> Database<T>(Func<EnigmaDbContext, Task<T>> work)
    {
        await using var scope = _services.CreateAsyncScope();
        return await work(scope.ServiceProvider.GetRequiredService<EnigmaDbContext>());
    }

    public Task Database(Func<EnigmaDbContext, Task> work)
    => Database(async context =>
    {
        await work(context);
        return true;
    });

    // Gives the owner of a test key a session, as a client or another node gets one by signing in.
    public async Task SignIn(string connectionId, string privateKey)
    {
        var sessions = Get<Enigma5.App.Hubs.Sessions.Contracts.ISessionManager>();
        var challenge = await sessions.AddPendingAsync(connectionId);
        var signedIn = await sessions.AuthenticateAsync(connectionId, TestKeys.PublicKeyOf(privateKey), TestSignatures.SignChallenge(privateKey, challenge!), null);
        if (!signedIn)
        {
            throw new InvalidOperationException("The test key could not sign in.");
        }
    }

    // A call of a hub method as a filter sees it: the hub, the method, its arguments and the calling connection.
    public Microsoft.AspNetCore.SignalR.HubInvocationContext HubCall(string connectionId, string methodName, params object?[] arguments)
    {
        var caller = NSubstitute.Substitute.For<Microsoft.AspNetCore.SignalR.HubCallerContext>();
        NSubstitute.SubstituteExtensions.Returns(caller.ConnectionId, connectionId);
        NSubstitute.SubstituteExtensions.Returns(caller.Items, new Dictionary<object, object?>
        {
            [Common.Constants.HubConnectionLocalIpKey] = System.Net.IPAddress.Loopback,
            [Common.Constants.HubConnectionLocalPortKey] = 8080
        });
        NSubstitute.SubstituteExtensions.Returns(caller.Features, new Microsoft.AspNetCore.Http.Features.FeatureCollection());
        var hub = ActivatorUtilities.CreateInstance<Enigma5.App.Hubs.RoutingHub>(_services);
        hub.Context = caller;
        return new(caller, _services, hub, typeof(Enigma5.App.Hubs.RoutingHub).GetMethod(methodName)!, arguments);
    }

    public T Create<T>() => ActivatorUtilities.CreateInstance<T>(_services);

    // Services of one scope, as one hub call or one request gets them. The caller disposes the scope.
    public AsyncServiceScope CreateScope() => _services.CreateAsyncScope();

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        _folder.Dispose();
    }
}
