using System.Text.Json.Serialization;

namespace Enigma5.App.Models.HubInvocation;

public class InvocationResult<T>
{
    public InvocationResult(T? data)
    {
        Data = data;
        Errors = [];
    }

    public InvocationResult()
    {
        Data = default;
        Errors = [];
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Data { get; set; }

    public virtual bool Success { get; set; }

    public IEnumerable<Error> Errors { get; set; }
}
