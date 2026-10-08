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

public class ValidateModelFilterTests
{
    [Fact]
    public async Task A_valid_request_passes()
    {
        await using var node = await TestNode.StartAsync();
        var call = node.HubCall("connection-2", nameof(RoutingHub.Cleanup2), new CleanupRequestDto(5));

        Assert.Equal(FilterCalls.Passed, await FilterCalls.Run(node.Create<ValidateModelFilter>(), call));
    }

    [Fact]
    public async Task A_request_that_breaks_a_rule_is_answered_with_the_errors_of_the_model()
    {
        await using var node = await TestNode.StartAsync();
        var call = node.HubCall("connection-2", nameof(RoutingHub.Cleanup2), new CleanupRequestDto(-1));

        var result = await FilterCalls.Run(node.Create<ValidateModelFilter>(), call);

        Assert.Equal(ValidationErrorsDto.INVALID_VALUE_FOR_PROPERTY, FilterCalls.SingleError(result));
        Assert.False(Assert.IsAssignableFrom<InvocationResultDto<object>>(result).Success);
    }

    [Fact]
    public async Task A_call_without_its_request_is_not_checked_by_this_filter()
    {
        await using var node = await TestNode.StartAsync();
        var call = node.HubCall("connection-2", nameof(RoutingHub.Cleanup2), [null]);

        // The filter only acts when the single argument is a request model; the hub method handles the rest.
        Assert.Equal(FilterCalls.Passed, await FilterCalls.Run(node.Create<ValidateModelFilter>(), call));
    }

    [Fact]
    public async Task A_method_without_request_passes()
    {
        await using var node = await TestNode.StartAsync();

        Assert.Equal(FilterCalls.Passed, await FilterCalls.Run(node.Create<ValidateModelFilter>(), node.HubCall("connection-2", nameof(RoutingHub.Pull))));
    }
}
