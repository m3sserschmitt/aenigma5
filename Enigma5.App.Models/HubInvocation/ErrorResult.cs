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
