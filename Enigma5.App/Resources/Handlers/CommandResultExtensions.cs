using System.Collections;
using Microsoft.AspNetCore.Http;

namespace Enigma5.App.Resources.Handlers;

public static class CommandResultExtensions
{
    public static bool IsSuccessNotNullResultValue<T>(this CommandResult<T>? result)
    => result is not null && result.Success && result.Value is not null;

    public static bool IsSuccessResult<T>(this CommandResult<T>? result)
    => result is not null && result.Success;

    public static IResult CreateGetResponse<T>(this CommandResult<T>? result)
    {
        if(!result.IsSuccessResult())
        {
            return Results.Problem(statusCode: 500);
        }

        if(result!.Value is IEnumerable || result.Value is not null) // collection (empty or not) or not null result
        {
            return Results.Ok(result.Value);
        }
        else if(result.Value is null) // not collection and null
        {
            return Results.NotFound();
        }

        return Results.Problem(statusCode: 500);
    }
}
