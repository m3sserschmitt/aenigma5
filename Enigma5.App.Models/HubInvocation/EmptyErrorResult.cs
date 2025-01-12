/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

namespace Enigma5.App.Models.HubInvocation;

public class EmptyErrorResult : ErrorResult<object>
{
    public EmptyErrorResult(IEnumerable<Error> errors) : base(null, errors) { }

    public EmptyErrorResult() : base() { }

    public static EmptyErrorResult Create(List<string> errors) => new(errors.Select(error => new Error(error)));

    public static EmptyErrorResult Create(string error) => Create([error]);
}
