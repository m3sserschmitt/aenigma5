namespace Enigma5.App.Resources.Handlers;

public class CommandResult<T>
{
    public CommandResult()
    {
        Value = default;
        Success = false;
    }

    protected CommandResult(bool success)
    {
        Value = default;
        Success = success;
    }

    protected CommandResult(T? value, bool success)
    {
        Value = value;
        Success = success;
    }

    public T? Value { get;set; }

    public bool Success { get; set; }

    public static CommandResult<V> CreateResultSuccess<V>() => new(true);

    public static CommandResult<V> CreateResultSuccess<V>(V? value) => new(value, true);

    public static CommandResult<V> CreateResultFailure<V>() => new(false);

    public static CommandResult<V> CreateResultFailure<V>(V? value) => new(value, false);
}

public class CommandResult: CommandResult<object>
{
    public CommandResult(): base() { }
    
    protected CommandResult(bool success): base(success) { }

    public static CommandResult CreateResultFailure() => new(false);
}
