namespace Enigma5.App.Models.HubInvocation;

public class SuccessResult<T> : InvocationResult<T>
{
    public SuccessResult(T? data) : base(data) { }

    public SuccessResult() { }

    public override bool Success => true;
}
