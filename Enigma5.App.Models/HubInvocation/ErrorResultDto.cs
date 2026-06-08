/*
    Aenigma - Federated messaging system
    Copyright © 2024-2026 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

public class ErrorResultDto<T>(T? data, HashSet<ErrorDto> errors) : InvocationResultDto<T>(data, errors, false)
{
    public static ErrorResultDto<T> Create(T? data, IEnumerable<string> errors) => new(data, [.. errors.Select(error => new ErrorDto(error))]);

    public static ErrorResultDto<T> Create(T? data, string error) => new(data, [new(error)]);
}

public class ErrorResultDto(HashSet<ErrorDto> errors) : ErrorResultDto<object>(null, errors)
{
    public static ErrorResultDto Create(List<string> errors) => new([.. errors.Select(error => new ErrorDto(error))]);

    public static ErrorResultDto Create(string error) => Create([error]);
}
