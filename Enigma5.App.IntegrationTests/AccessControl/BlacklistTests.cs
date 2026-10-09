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

using System.Net;
using Enigma5.App.Models;
using Enigma5.Tests.Base;

namespace Enigma5.App.IntegrationTests.AccessControl;

// The node runs with the shipped rules for its public endpoint.
public class BlacklistTests(NodeAFixture fixture) : IClassFixture<NodeAFixture>
{
    private readonly NodeProcess _node = fixture.Node;

    [Fact]
    public async Task The_dashboard_is_closed_on_the_public_endpoint_and_open_on_the_control_endpoint()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _node.Public.GetAsync("/Dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _node.Control.GetAsync("/Dashboard")).StatusCode);
    }

    [Fact]
    public async Task The_connection_of_the_dashboard_is_closed_on_the_public_endpoint_and_open_on_the_control_endpoint()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _node.Public.PostAsync("/_blazor/negotiate?negotiateVersion=1", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _node.Control.PostAsync("/_blazor/negotiate?negotiateVersion=1", null)).StatusCode);
    }

    [Fact]
    public async Task The_other_endpoints_are_open_on_both()
    {
        Assert.Equal(HttpStatusCode.OK, (await _node.Public.GetAsync("/Info")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _node.Control.GetAsync("/Info")).StatusCode);
    }

    [Fact]
    public async Task The_hub_method_TriggerBroadcast_is_refused_on_the_public_endpoint_with_a_general_error()
    {
        await using var client = await HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey3);

        var result = await client.Invoke<bool>("TriggerBroadcast", new TriggerBroadcastRequestDto([]));

        Assert.False(result.Success);
        Assert.Equal("Internal error", result.Error);
    }

    [Fact]
    public async Task The_hub_method_TriggerBroadcast_is_not_refused_by_the_rule_on_the_control_endpoint()
    {
        await using var client = await HubClient.SignedInAsync(_node.ControlUrl, TestKeys.PrivateKey3);

        var result = await client.Invoke<bool>("TriggerBroadcast", new TriggerBroadcastRequestDto([]));

        Assert.NotEqual("Internal error", result.Error);
    }
}
