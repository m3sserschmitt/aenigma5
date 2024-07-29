namespace Enigma5.App.Models;

public class InvocationResult<T>
{
    public T? Data { get; set; }

    public IEnumerable<Error> Errors { get; set; }

    public bool Success { get; set; }

    public InvocationResult(T? data, IEnumerable<Error> errors)
    {
        Data = data;
        Errors = errors;
        Success = false;
    }

    public InvocationResult(T? data)
    {
        Data = data;
        Errors = [];
        Success = true;
    }

    public InvocationResult()
    { 
        Data = default;
        Errors = [];
        Success = false;
    }
}
