namespace Enigma5.App.Models.HubInvocation;

public class EmptyErrorResult : ErrorResult<object>
{
    public EmptyErrorResult(IEnumerable<Error> errors) : base(null, errors) { }

    public EmptyErrorResult() : base() { }

    public static EmptyErrorResult Create(List<string> errors) => new(errors.Select(error => new Error(error)));

    public static EmptyErrorResult Create(string error) => Create([error]);
}
