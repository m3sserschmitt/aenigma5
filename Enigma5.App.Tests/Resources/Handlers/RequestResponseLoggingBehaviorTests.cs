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

using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Handlers;
using Enigma5.Tests.Base;

namespace Enigma5.App.Tests.Resources.Handlers;

public class RequestResponseLoggingBehaviorTests
{
    private readonly CapturingLogger<RequestResponseLoggingBehavior<CleanupGraphCommand, CommandResult<int>>> _logger = new();

    private RequestResponseLoggingBehavior<CleanupGraphCommand, CommandResult<int>> Behavior => new(_logger);

    [Fact]
    public async Task The_result_of_the_handler_is_passed_on()
    {
        var result = await Behavior.Handle(new CleanupGraphCommand(), () => Task.FromResult(CommandResult.CreateResultSuccess(3)), default);

        Assert.True(result.Success);
        Assert.Equal(3, result.Value);
        Assert.Empty(_logger.Errors);
    }

    [Fact]
    public async Task An_exception_in_the_handler_becomes_a_failure_and_is_logged()
    {
        var result = await Behavior.Handle(new CleanupGraphCommand(), () => throw new InvalidOperationException("broken"), default);

        Assert.False(result.Success);
        Assert.IsType<InvalidOperationException>(Assert.Single(_logger.Errors).Exception);
    }
}
