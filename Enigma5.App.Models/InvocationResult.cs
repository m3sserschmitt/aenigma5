namespace Enigma5.App.Models;

public class InvocationResult<T>
{
    public T? Result { get; set; }

    public IEnumerable<Error> Errors { get; set; }

    public bool Success { get; set; }

    public InvocationResult(T? data, IEnumerable<Error> errors)
    {
        Result = data;
        Errors = errors;
        Success = false;
    }

    public InvocationResult(T? data)
    {
        Result = data;
        Errors = [];
        Success = true;
    }

    public InvocationResult()
    { 
        Result = default;
        Errors = [];
        Success = false;
    }
}
