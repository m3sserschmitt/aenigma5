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

namespace Enigma5.App.Models.Tests;

public class ErrorDtoTests
{
    [Fact]
    public void Two_errors_are_equal_when_their_messages_are_equal()
    {
        var first = new ErrorDto("message", ["A"]);
        var second = new ErrorDto("message", ["B"]);

        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.False(first == new ErrorDto("other", ["A"]));
    }

    [Fact]
    public void A_set_keeps_one_error_for_each_message()
    {
        var errors = new HashSet<ErrorDto> { new("message", ["A"]), new("message", ["B"]), new("other") };

        Assert.Equal(2, errors.Count);
    }
}
