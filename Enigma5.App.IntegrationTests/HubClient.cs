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
using System.Text.Json;
using Enigma5.App.Models;
using Enigma5.Tests.Base;
using Microsoft.AspNetCore.SignalR.Client;

namespace Enigma5.App.IntegrationTests;

// What a hub method answers.
public sealed record HubResult<T>(T? Data, bool Success, List<HubError>? Errors)
{
    public string? Error => Errors is { Count: 1 } ? Errors[0].Message : Errors is null or { Count: 0 } ? null : string.Join("; ", Errors.Select(error => error.Message));
}

public sealed record HubError(string? Message, List<string>? Properties);

// A connection to the hub of a node, as a client or another node opens it.
public sealed class HubClient : IAsyncDisposable
{
    private readonly HubConnection _connection;

    // Messages and vertices the node has pushed to this connection.
    public ConcurrentQueue<JsonElement> RoutedMessages { get; } = new();

    public ConcurrentQueue<JsonElement> Broadcasts { get; } = new();

    private HubClient(string nodeUrl)
    {
        _connection = new HubConnectionBuilder().WithUrl($"{nodeUrl}/OnionRouting").Build();
        _connection.On<JsonElement>("RouteMessage", RoutedMessages.Enqueue);
        _connection.On<JsonElement>("Broadcast", Broadcasts.Enqueue);
    }

    public static async Task<HubClient> ConnectAsync(string nodeUrl)
    {
        var client = new HubClient(nodeUrl);
        await client._connection.StartAsync();
        return client;
    }

    // Connects and signs in with one of the test keys.
    public static async Task<HubClient> SignedInAsync(string nodeUrl, string privateKey)
    {
        var client = await ConnectAsync(nodeUrl);
        var signedIn = await client.SignInAsync(privateKey);
        return signedIn.Success ? client : throw new InvalidOperationException($"Sign-in failed: {signedIn.Error}");
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // An answer that reports errors carries no data, whatever type the method normally returns.
    private static HubResult<T> Read<T>(HubResult<JsonElement> raw)
    => new(raw.Data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? default : raw.Data.Deserialize<T>(Json), raw.Success, raw.Errors);

    public async Task<HubResult<T>> Invoke<T>(string method) => Read<T>(await _connection.InvokeAsync<HubResult<JsonElement>>(method));

    public async Task<HubResult<T>> Invoke<T>(string method, object? request) => Read<T>(await _connection.InvokeAsync<HubResult<JsonElement>>(method, request));

    public async Task<HubResult<bool>> SignInAsync(string privateKey)
    {
        var challenge = await Invoke<string>("GenerateToken");
        return await Invoke<bool>("Authenticate", new AuthenticationRequestDto(TestKeys.PublicKeyOf(privateKey), TestSignatures.SignChallenge(privateKey, challenge.Data!)));
    }

    public Task<HubResult<bool>> Route(string? uuid, params string?[] onions) => Invoke<bool>("RouteMessage", new RoutingRequestDto([.. onions], uuid));

    public Task<HubResult<List<PendingMessageDto>>> Pull2(long? afterId = null) => Invoke<List<PendingMessageDto>>("Pull2", new PullRequestDto(afterId));

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
