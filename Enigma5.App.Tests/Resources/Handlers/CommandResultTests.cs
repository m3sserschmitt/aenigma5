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

using Enigma5.App.Resources.Handlers;

namespace Enigma5.App.Tests.Resources.Handlers;

public class CommandResultTests
{
    [Fact]
    public void A_new_result_is_a_failure_without_value()
    {
        var result = new CommandResult<string>();

        Assert.False(result.Success);
        Assert.Null(result.Value);
    }

    [Fact]
    public void The_factory_methods_set_success_and_value()
    {
        Assert.True(CommandResult.CreateResultSuccess("value").Success);
        Assert.Equal("value", CommandResult.CreateResultSuccess("value").Value);
        Assert.True(CommandResult.CreateResultSuccess<string>().Success);
        Assert.Null(CommandResult.CreateResultSuccess<string>().Value);
        Assert.False(CommandResult.CreateResultFailure<string>().Success);
        Assert.Equal("value", CommandResult.CreateResultFailure<string>("value").Value);
    }

    [Fact]
    public void ToFailure_clears_the_value()
    {
        var result = CommandResult.CreateResultSuccess("value");

        result.ToFailure();

        Assert.False(result.Success);
        Assert.Null(result.Value);
    }
}
