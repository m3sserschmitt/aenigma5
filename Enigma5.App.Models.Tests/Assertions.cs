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

internal static class Assertions
{
    // The set of errors holds exactly one error, with this message and for this property.
    public static void SingleError(HashSet<ErrorDto> errors, string message, string property)
    {
        var error = Assert.Single(errors);
        Assert.Equal(message, error.Message);
        Assert.Equal([property], error.Properties);
    }
}
