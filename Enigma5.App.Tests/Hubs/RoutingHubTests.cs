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

using Enigma5.App.Hubs;
using Enigma5.App.Hubs.Sessions.Contracts;
using Enigma5.App.Models;
using Enigma5.App.Models.HubInvocation;
using Enigma5.App.Resources.Commands;
using Enigma5.Tests.Base;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Tests.Hubs;

// The node has key 1. The filters of the hub have tests of their own; here the methods are called directly.
public class RoutingHubTests
{
    private const string Connection = "connection-1";

    private const string OtherConnection = "connection-2";

    private static string SingleError<T>(InvocationResultDto<T> result)
    {
        Assert.False(result.Success);
        return Assert.Single(result.Errors).Message!;
    }

    private static async Task Store(TestNode node, string destination, int count)
    {
        for (var index = 0; index < count; index++)
        {
            Assert.True((await node.Send(new CreatePendingMessageCommand(destination, $"content {index}", null))).Success);
        }
    }

    private static Task<int> Confirmed(TestNode node) => node.Database(context => context.Messages.CountAsync(message => message.Sent));

    private static Task<List<string>> NeighborsOfTheNode(TestNode node)
    => node.Database(async context => (await node.Send(new Enigma5.App.Resources.Queries.GetNeighborAddressesQuery())).Value!.ToList());

    [Fact]
    public async Task GenerateToken_and_Authenticate_give_the_connection_a_session()
    {
        await using var node = await TestNode.StartAsync();
        await using var connection = new HubConnection(node, Connection);

        var token = await connection.Hub.GenerateToken();
        var signedIn = await connection.Hub.Authenticate(new AuthenticationRequestDto(TestKeys.PublicKey3, TestSignatures.SignChallenge(TestKeys.PrivateKey3, token.Data!)));

        Assert.True(token.Success);
        Assert.True(signedIn.Success);
        Assert.True(signedIn.Data);
        Assert.Equal(Connection, await node.Get<ISessionManager>().TryGetConnectionIdAsync(TestKeys.Address3));
    }

    [Fact]
    public async Task Authenticate_refuses_a_token_signed_with_another_key()
    {
        await using var node = await TestNode.StartAsync();
        await using var connection = new HubConnection(node, Connection);
        var token = await connection.Hub.GenerateToken();

        var result = await connection.Hub.Authenticate(new AuthenticationRequestDto(TestKeys.PublicKey3, TestSignatures.SignChallenge(TestKeys.PrivateKey2, token.Data!)));

        Assert.Equal(InvocationErrors.INVALID_NONCE_SIGNATURE, SingleError(result));
        Assert.Null(await node.Get<ISessionManager>().TryGetConnectionIdAsync(TestKeys.Address3));
    }

    [Fact]
    public async Task Authenticate_refuses_a_connection_that_asked_for_no_token()
    {
        await using var node = await TestNode.StartAsync();
        await using var connection = new HubConnection(node, Connection);

        var result = await connection.Hub.Authenticate(new AuthenticationRequestDto(TestKeys.PublicKey3, TestSignatures.SignChallenge(TestKeys.PrivateKey3, "bm8gdG9rZW4=")));

        Assert.Equal(InvocationErrors.INVALID_NONCE_SIGNATURE, SingleError(result));
    }

    [Fact]
    public async Task GetLocalVertex_returns_the_vertex_of_the_node()
    {
        await using var node = await TestNode.StartAsync();
        await using var connection = new HubConnection(node, Connection);

        var result = await connection.Hub.GetLocalVertex();

        Assert.True(result.Success);
        Assert.Equal(TestKeys.Address1, result.Data!.Neighborhood!.Address);
    }

    [Fact]
    public async Task Pull_and_Pull2_return_the_messages_of_the_caller_only()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address3, 3);
        await Store(node, TestKeys.Address2, 2);
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.ClientAddress = TestKeys.Address3;

#pragma warning disable CS0618 // The old method is still served to old clients.
        var all = await connection.Hub.Pull();
#pragma warning restore CS0618
        var page = await connection.Hub.Pull2(new PullRequestDto());

        Assert.Equal(3, all.Data!.Count);
        Assert.Equal(3, page.Data!.Count);
        Assert.All(page.Data, message => Assert.Equal(TestKeys.Address3, message.Destination));
    }

    [Fact]
    public async Task Pull2_continues_after_the_given_message()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address3, 3);
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.ClientAddress = TestKeys.Address3;
        var first = await connection.Hub.Pull2(new PullRequestDto());

        var rest = await connection.Hub.Pull2(new PullRequestDto(first.Data![0].Id));

        Assert.Equal(first.Data.Skip(1).Select(message => message.Id), rest.Data!.Select(message => message.Id));
    }

    [Fact]
    public async Task Pull_Pull2_Cleanup_and_Cleanup2_fail_when_the_caller_has_no_address()
    {
        await using var node = await TestNode.StartAsync();
        await using var connection = new HubConnection(node, Connection);

#pragma warning disable CS0618
        Assert.Equal(InvocationErrors.INTERNAL_ERROR, SingleError(await connection.Hub.Pull()));
        Assert.Equal(InvocationErrors.INTERNAL_ERROR, SingleError(await connection.Hub.Cleanup()));
#pragma warning restore CS0618
        Assert.Equal(InvocationErrors.INTERNAL_ERROR, SingleError(await connection.Hub.Pull2(new PullRequestDto())));
        Assert.Equal(InvocationErrors.INTERNAL_ERROR, SingleError(await connection.Hub.Cleanup2(new CleanupRequestDto())));
    }

    [Fact]
    public async Task Cleanup_confirms_nothing_on_a_connection_that_has_pulled_nothing()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address3, 3);
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.ClientAddress = TestKeys.Address3;

#pragma warning disable CS0618
        var old = await connection.Hub.Cleanup();
#pragma warning restore CS0618
        var current = await connection.Hub.Cleanup2(new CleanupRequestDto(long.MaxValue));

        Assert.True(old.Success);
        Assert.True(current.Success);
        Assert.Equal(0, await Confirmed(node));
    }

    [Fact]
    public async Task Cleanup_confirms_the_messages_the_connection_has_pulled_and_none_that_came_later()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address3, 2);
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.ClientAddress = TestKeys.Address3;
        await connection.Hub.Pull2(new PullRequestDto());
        await Store(node, TestKeys.Address3, 1);

#pragma warning disable CS0618
        var result = await connection.Hub.Cleanup();
#pragma warning restore CS0618

        Assert.True(result.Success);
        Assert.Equal(2, await Confirmed(node));
    }

    [Fact]
    public async Task Cleanup2_confirms_up_to_the_given_message()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address3, 3);
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.ClientAddress = TestKeys.Address3;
        var pulled = await connection.Hub.Pull2(new PullRequestDto());

        var result = await connection.Hub.Cleanup2(new CleanupRequestDto(pulled.Data![1].Id));

        Assert.True(result.Success);
        Assert.Equal(2, await Confirmed(node));
    }

    [Fact]
    public async Task Cleanup2_never_confirms_beyond_what_the_connection_has_pulled()
    {
        await using var node = await TestNode.StartAsync();
        await Store(node, TestKeys.Address3, 2);
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.ClientAddress = TestKeys.Address3;
        await connection.Hub.Pull2(new PullRequestDto());
        await Store(node, TestKeys.Address3, 2);

        var result = await connection.Hub.Cleanup2(new CleanupRequestDto(long.MaxValue));

        Assert.True(result.Success);
        Assert.Equal(2, await Confirmed(node));
    }

    [Fact]
    public async Task Broadcast_from_a_connected_node_that_lists_this_node_makes_it_a_neighbor_and_sends_it_the_new_local_vertex()
    {
        await using var node = await TestNode.StartAsync();
        await node.SignIn(Connection, TestKeys.PrivateKey2);
        await using var connection = new HubConnection(node, Connection);

        var result = await connection.Hub.Broadcast(TestVertices.Signed(TestKeys.PrivateKey2, TestKeys.Address1));

        Assert.True(result.Success);
        Assert.Contains(TestKeys.Address2, await NeighborsOfTheNode(node));
        // The changed local vertex and the received one are sent to the new neighbor, in no fixed order.
        (string ConnectionId, string Method, object? Argument)[] sent = [await connection.NextSent(), await connection.NextSent()];
        Assert.All(sent, item => Assert.Equal((Connection, nameof(RoutingHub.Broadcast)), (item.ConnectionId, item.Method)));
        var owners = sent.Select(item => Assert.IsType<VertexBroadcastRequestDto>(item.Argument).PublicKey).ToList();
        Assert.Contains(TestKeys.PublicKey1, owners);
        Assert.Contains(TestKeys.PublicKey2, owners);
    }

    // The graph does not take such a vertex, and the hub has nothing to pass on. The caller is not told.
    [Fact]
    public async Task Broadcast_of_a_vertex_that_does_not_match_its_signature_changes_nothing_and_sends_nothing()
    {
        await using var node = await TestNode.StartAsync();
        await node.SignIn(Connection, TestKeys.PrivateKey2);
        await using var connection = new HubConnection(node, Connection);
        var signedByKey3 = TestVertices.Signed(TestKeys.PrivateKey3, TestKeys.Address1);

        var result = await connection.Hub.Broadcast(new VertexBroadcastRequestDto(TestKeys.PublicKey2, signedByKey3.SignedData));

        Assert.True(result.Success);
        Assert.Empty(await NeighborsOfTheNode(node));
        await Task.Delay(100);
        Assert.Empty(connection.Sent);
    }

    [Fact]
    public async Task Broadcast_without_a_public_key_fails()
    {
        await using var node = await TestNode.StartAsync();
        await using var connection = new HubConnection(node, Connection);

        var result = await connection.Hub.Broadcast(new VertexBroadcastRequestDto("not a key", TestVertices.Signed(TestKeys.PrivateKey2).SignedData));

        Assert.Equal(InvocationErrors.BROADCAST_HANDLING_ERROR, SingleError(result));
    }

    [Fact]
    public async Task TriggerBroadcast_adds_a_connected_node_as_neighbor_and_sends_it_the_local_vertex()
    {
        await using var node = await TestNode.StartAsync();
        await node.SignIn(OtherConnection, TestKeys.PrivateKey2);
        await using var connection = new HubConnection(node, Connection);

        var result = await connection.Hub.TriggerBroadcast(new TriggerBroadcastRequestDto([TestKeys.Address2]));

        Assert.True(result.Success);
        Assert.Contains(TestKeys.Address2, await NeighborsOfTheNode(node));
        var sent = await connection.NextSent();
        Assert.Equal(OtherConnection, sent.ConnectionId);
        Assert.Equal(nameof(RoutingHub.Broadcast), sent.Method);
    }

    [Fact]
    public async Task TriggerBroadcast_does_not_add_an_address_that_is_not_connected()
    {
        await using var node = await TestNode.StartAsync();
        await using var connection = new HubConnection(node, Connection);

        var result = await connection.Hub.TriggerBroadcast(new TriggerBroadcastRequestDto([TestKeys.Address2]));

        Assert.True(result.Success);
        Assert.Empty(await NeighborsOfTheNode(node));
        await Task.Delay(100);
        Assert.Empty(connection.Sent);
    }

    [Fact]
    public async Task TriggerBroadcast_with_something_that_is_not_an_address_reports_it_and_sends_nothing()
    {
        await using var node = await TestNode.StartAsync();
        await using var connection = new HubConnection(node, Connection);

        var result = await connection.Hub.TriggerBroadcast(new TriggerBroadcastRequestDto(["not an address"]));

        Assert.Equal(InvocationErrors.BROADCAST_TRIGGERING_WARNING, SingleError(result));
        Assert.True(result.Data);
        Assert.Empty(connection.Sent);
    }

    [Fact]
    public async Task RouteMessage_stores_the_parsed_onion_for_its_next_address()
    {
        await using var node = await TestNode.StartAsync();
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.Next = TestKeys.Address3;
        connection.Hub.Content = [1, 2, 3];

        var result = await connection.Hub.RouteMessage(new RoutingRequestDto(["unused"]));

        Assert.True(result.Success);
        var stored = await node.Database(context => context.Messages.SingleAsync());
        Assert.Equal(TestKeys.Address3, stored.Destination);
        Assert.Equal(Convert.ToBase64String([1, 2, 3]), stored.Content);
        Assert.False(stored.Sent);
        Assert.Empty(connection.Sent);
    }

    [Fact]
    public async Task RouteMessage_stores_the_message_under_the_uuid_of_the_request_and_only_once()
    {
        await using var node = await TestNode.StartAsync();
        var uuid = Guid.NewGuid().ToString();
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.Next = TestKeys.Address3;
        connection.Hub.Content = [1, 2, 3];
        connection.Hub.Uuid = uuid;

        var first = await connection.Hub.RouteMessage(new RoutingRequestDto(["unused"], uuid));
        var second = await connection.Hub.RouteMessage(new RoutingRequestDto(["unused"], uuid));

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(uuid, (await node.Database(context => context.Messages.SingleAsync())).Uuid);
    }

    [Fact]
    public async Task RouteMessage_also_sends_the_message_with_its_uuid_to_a_connected_destination()
    {
        await using var node = await TestNode.StartAsync();
        await using var connection = new HubConnection(node, Connection);
        connection.Hub.Next = TestKeys.Address3;
        connection.Hub.Content = [1, 2, 3];
        connection.Hub.DestinationConnectionId = OtherConnection;

        var result = await connection.Hub.RouteMessage(new RoutingRequestDto(["unused"]));

        Assert.True(result.Success);
        var stored = await node.Database(context => context.Messages.SingleAsync());
        var sent = await connection.NextSent();
        Assert.Equal(OtherConnection, sent.ConnectionId);
        Assert.Equal(nameof(RoutingHub.RouteMessage), sent.Method);
        var request = Assert.IsType<RoutingRequestDto>(sent.Argument);
        Assert.Equal([Convert.ToBase64String([1, 2, 3])], request.Payloads!);
        Assert.Equal(stored.Uuid, request.Uuid);
    }

    [Fact]
    public async Task RouteMessage_fails_when_no_onion_was_parsed()
    {
        await using var node = await TestNode.StartAsync();
        await using var connection = new HubConnection(node, Connection);

        var result = await connection.Hub.RouteMessage(new RoutingRequestDto(["unused"]));

        Assert.Equal(InvocationErrors.ONION_ROUTING_FAILED, SingleError(result));
        Assert.Equal(0, await node.Database(context => context.Messages.CountAsync()));
    }

    [Fact]
    public async Task OnConnectedAsync_keeps_the_local_endpoint_and_the_impersonation_header_of_the_connection()
    {
        await using var node = await TestNode.StartAsync();
        await using var connection = new HubConnection(node, Connection);
        connection.HttpContext.Connection.LocalIpAddress = System.Net.IPAddress.Loopback;
        connection.HttpContext.Connection.LocalPort = 8080;
        connection.HttpContext.Request.Headers[Common.Constants.XImpersonateServiceHeaderKey] = TestKeys.Address2;

        await connection.Hub.OnConnectedAsync();

        var items = connection.Hub.Context.Items;
        Assert.Equal(System.Net.IPAddress.Loopback, items[Common.Constants.HubConnectionLocalIpKey]);
        Assert.Equal(8080, items[Common.Constants.HubConnectionLocalPortKey]);
        Assert.Equal(TestKeys.Address2, items[Common.Constants.XImpersonateServiceHeaderKey]?.ToString());
    }

    [Fact]
    public async Task OnDisconnectedAsync_ends_the_session_and_removes_the_caller_from_the_neighbors()
    {
        await using var node = await TestNode.StartAsync();
        await node.SignIn(Connection, TestKeys.PrivateKey2);
        await using var connection = new HubConnection(node, Connection);
        Assert.True((await connection.Hub.Broadcast(TestVertices.Signed(TestKeys.PrivateKey2, TestKeys.Address1))).Success);

        await connection.Hub.OnDisconnectedAsync(null);

        Assert.Null(await node.Get<ISessionManager>().TryGetConnectionIdAsync(TestKeys.Address2));
        Assert.DoesNotContain(TestKeys.Address2, await NeighborsOfTheNode(node));
    }

    [Fact]
    public async Task OnDisconnectedAsync_of_a_connection_without_session_changes_nothing()
    {
        await using var node = await TestNode.StartAsync();
        await node.SignIn(OtherConnection, TestKeys.PrivateKey2);
        await using var connection = new HubConnection(node, Connection);

        await connection.Hub.OnDisconnectedAsync(null);

        Assert.Equal(OtherConnection, await node.Get<ISessionManager>().TryGetConnectionIdAsync(TestKeys.Address2));
    }
}
