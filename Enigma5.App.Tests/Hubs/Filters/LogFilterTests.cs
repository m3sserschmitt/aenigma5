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
using Enigma5.Tests.Base;
using Microsoft.Extensions.Logging;

namespace Enigma5.App.Tests.Hubs.Filters;

// Error events are written also on packaged nodes, so they must not carry the arguments of a call.
public class LogFilterTests
{
    private readonly CapturingLogger<LogFilter> _logger = new();

    // Arguments are logged destructured ("@"), so the event holds them under "@HubMethodArguments".
    private static bool HasArguments(CapturingLogger<LogFilter>.Entry entry)
    => entry.Properties.Keys.Any(key => key.TrimStart('@') == Common.Constants.Serilog.HubMethodArgumentsKey);

    private async Task Call(Func<Microsoft.AspNetCore.SignalR.HubInvocationContext, ValueTask<object?>> hubMethod)
    {
        await using var node = await TestNode.StartAsync();
        var call = node.HubCall("connection-2", nameof(RoutingHub.RouteMessage), new RoutingRequestDto(["b25pb24="]));
        await new LogFilter(_logger).InvokeMethodAsync(call, hubMethod);
    }

    // A call that throws has no result either, so it gives two error events.
    [Fact]
    public async Task A_call_that_throws_is_logged_as_an_error_without_its_arguments()
    {
        await Call(_ => throw new InvalidOperationException("broken"));

        Assert.Contains(_logger.Errors, error => error.Exception is InvalidOperationException
            && (string?)error.Properties[Common.Constants.Serilog.HubMethodNameKey] == nameof(RoutingHub.RouteMessage));
        Assert.DoesNotContain(_logger.Errors, HasArguments);
    }

    [Fact]
    public async Task A_call_without_result_is_logged_as_an_error_without_its_arguments()
    {
        await Call(_ => ValueTask.FromResult<object?>(null));

        Assert.False(HasArguments(Assert.Single(_logger.Errors)));
    }

    [Fact]
    public async Task The_arguments_are_logged_at_the_debug_level_only()
    {
        await Call(_ => throw new InvalidOperationException("broken"));

        Assert.Contains(_logger.Entries, entry => entry.Level == LogLevel.Debug && HasArguments(entry));
        Assert.DoesNotContain(_logger.Entries, entry => entry.Level > LogLevel.Debug && HasArguments(entry));
    }
}
