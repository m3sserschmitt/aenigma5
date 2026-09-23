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

using Enigma5.App.Resources.Contracts;

namespace Enigma5.App.Resources.Handlers;

public class CommandResult<T> : ICommandResult
{
    public CommandResult()
    {
        _value = default;
        _success = false;
    }

    public CommandResult(T? value, bool success)
    {
        _value = value;
        _success = success;
    }

    private T? _value;

    private bool _success;

    public T? Value { get => _value; }

    public bool Success { get => _success; }

    public static CommandResult<V> CreateResultSuccess<V>() => new(default, true);

    public static CommandResult<V> CreateResultSuccess<V>(V? value) => new(value, true);

    public static CommandResult<V> CreateResultFailure<V>() => new(default, false);

    public static CommandResult<V> CreateResultFailure<V>(V? value) => new(value, false);

    public void ToFailure()
    {
        _value = default;
        _success = false;
    }
}

public class CommandResult : CommandResult<object> { }
