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

using System.Collections;

namespace Enigma5.App.Resources.Handlers;

public static class CommandResultExtensions
{
    public static bool IsNotNullResultValue<T>(this CommandResult<T>? result)
    => result is not null && result.Value is not null;

    public static bool IsSuccessNotNullResultValue<T>(this CommandResult<T>? result)
    => result is not null && result.Success && result.Value is not null;

    public static bool IsSuccessResult<T>(this CommandResult<T>? result)
    => result is not null && result.Success;

    public static IResult CreateGetResponse<T>(this CommandResult<T>? result)
    {
        if (!result.IsSuccessResult())
        {
            return Results.Problem(statusCode: 500);
        }

        if (result!.Value is IEnumerable || result.Value is not null) // collection (empty or not) or not null result
        {
            return Results.Ok(result.Value);
        }
        else if (result.Value is null) // not collection and null
        {
            return Results.NotFound();
        }

        return Results.Problem(statusCode: 500);
    }

    public static IResult CreatePostResponse<T>(this CommandResult<T>? result)
    => result.IsSuccessNotNullResultValue() ? Results.Ok(result!.Value) : Results.Problem(statusCode: 500);

    public static IResult CreatePutResponse<T>(this CommandResult<T>? result)
    => result.IsSuccessResult() ? Results.Ok() : Results.Problem(statusCode: 500);
}
