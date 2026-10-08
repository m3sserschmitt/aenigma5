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
using Enigma5.App.Hubs.Filters;
using Enigma5.App.Models;
using Enigma5.App.Models.HubInvocation;
using Enigma5.Tests.Base;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Tests.Hubs.Filters;

public class AuthenticatedFilterTests
{
    [Fact]
    public async Task A_signed_in_connection_passes_and_the_hub_learns_its_address()
    {
        await using var node = await TestNode.StartAsync();
        await node.SignIn("connection-2", TestKeys.PrivateKey2);
        var call = node.HubCall("connection-2", nameof(RoutingHub.Pull));

        var result = await FilterCalls.Run(node.Create<AuthenticatedFilter>(), call);

        Assert.Equal(FilterCalls.Passed, result);
        Assert.Equal(TestKeys.Address2, ((RoutingHub)call.Hub).ClientAddress);
    }

    [Fact]
    public async Task A_connection_that_is_not_signed_in_is_refused()
    {
        await using var node = await TestNode.StartAsync();

        var result = await FilterCalls.Run(node.Create<AuthenticatedFilter>(), node.HubCall("connection-2", nameof(RoutingHub.Pull)));

        Assert.Equal(InvocationErrors.AUTHENTICATION_REQUIRED, FilterCalls.SingleError(result));
    }

    [Fact]
    public async Task A_connection_whose_session_was_taken_over_is_refused()
    {
        await using var node = await TestNode.StartAsync();
        await node.SignIn("connection-2", TestKeys.PrivateKey2);
        await node.SignIn("connection-3", TestKeys.PrivateKey2);

        var result = await FilterCalls.Run(node.Create<AuthenticatedFilter>(), node.HubCall("connection-2", nameof(RoutingHub.Pull)));

        Assert.Equal(InvocationErrors.AUTHENTICATION_REQUIRED, FilterCalls.SingleError(result));
    }

    [Fact]
    public async Task A_method_that_needs_no_sign_in_passes_without_one()
    {
        await using var node = await TestNode.StartAsync();

        var result = await FilterCalls.Run(node.Create<AuthenticatedFilter>(), node.HubCall("connection-2", nameof(RoutingHub.GenerateToken)));

        Assert.Equal(FilterCalls.Passed, result);
    }
}
