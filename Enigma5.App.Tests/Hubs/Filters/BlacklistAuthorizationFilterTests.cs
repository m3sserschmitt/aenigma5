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

public class BlacklistAuthorizationFilterTests
{
    private static readonly (string, string?)[] Rule =
    [
        ("HubBlacklists:0:Endpoint", "http://127.0.0.1:8080"),
        ("HubBlacklists:0:Items:0:Methods:0", "TriggerBroadcast")
    ];

    [Fact]
    public async Task A_method_named_by_a_rule_of_the_endpoint_is_refused_with_a_general_error()
    {
        await using var node = await TestNode.StartAsync(Rule);
        var call = node.HubCall("connection-2", nameof(RoutingHub.TriggerBroadcast), new TriggerBroadcastRequestDto());

        var result = await FilterCalls.Run(node.Create<BlacklistAuthorizationFilter>(), call);

        // The answer does not say that a rule exists.
        Assert.Equal(InvocationErrors.INTERNAL_ERROR, FilterCalls.SingleError(result));
    }

    [Fact]
    public async Task Another_method_passes()
    {
        await using var node = await TestNode.StartAsync(Rule);

        Assert.Equal(FilterCalls.Passed, await FilterCalls.Run(node.Create<BlacklistAuthorizationFilter>(), node.HubCall("connection-2", nameof(RoutingHub.GenerateToken))));
    }

    [Fact]
    public async Task Without_rules_every_method_passes()
    {
        await using var node = await TestNode.StartAsync();
        var call = node.HubCall("connection-2", nameof(RoutingHub.TriggerBroadcast), new TriggerBroadcastRequestDto());

        Assert.Equal(FilterCalls.Passed, await FilterCalls.Run(node.Create<BlacklistAuthorizationFilter>(), call));
    }
}
