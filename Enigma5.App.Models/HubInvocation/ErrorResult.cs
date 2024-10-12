/*
    Aenigma - Onion Routing based messaging application
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

public class ErrorResult<T> : InvocationResult<T>
{
    public ErrorResult(T? data, IEnumerable<Error> errors) : base(data)
    {
        Errors = errors;
    }

    public ErrorResult() { }

    public override bool Success => false;

    public static ErrorResult<T> Create(T? data, IEnumerable<string> errors) => new(data, errors.Select(error => new Error(error)));

    public static ErrorResult<T> Create(T? data, string error) => new(data, [new(error)]);
}
