namespace Enigma5.App.Resources.Handlers;

public static class CommandResultExtensions
{
    public static bool IsSuccessNotNullResulValue<T>(this CommandResult<T>? result)
    => result is not null && result.Success && result.Value is not null;

    public static bool IsSuccessResult<T>(this CommandResult<T>? result)
    => result is not null && result.Success;
}
