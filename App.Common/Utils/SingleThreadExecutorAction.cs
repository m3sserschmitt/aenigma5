namespace Enigma5.App.Common.Utils;

public class SingleThreadExecutorAction(Action action)
{
    private readonly Action _noReturnValueAction = action;

    public Exception? Exception { get; protected set; }

    public virtual bool HasReturnValue => false;

    public virtual void Invoke()
    {
        try
        {
            _noReturnValueAction.Invoke();
        }
        catch (Exception ex)
        {
            Exception = ex;
        }
    }

    public static implicit operator SingleThreadExecutorAction(Action action)
    {
        return new SingleThreadExecutorAction(action);
    }
}

public class SingleThreadExecutorAction<T>(Func<T> action) : SingleThreadExecutorAction(() => { })
{
    private readonly Func<T> _actionWithReturnValue = action;

    public T? Result { get; private set; }

    public override bool HasReturnValue => true;

    public override void Invoke()
    {
        try
        {
            Result = _actionWithReturnValue.Invoke();
        }
        catch (Exception ex)
        {
            Exception = ex;
        }
    }

    public static implicit operator SingleThreadExecutorAction<T>(Func<T> action)
    {
        return new SingleThreadExecutorAction<T>(action);
    }
}
