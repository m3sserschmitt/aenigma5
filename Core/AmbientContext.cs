namespace Enigma5.Core;

public abstract class AmbientContext<T> : IDisposable
where T : AmbientContext<T>, new()
{
    private static ThreadLocal<Stack<T>> stack = new ThreadLocal<Stack<T>>(
        () => new Stack<T>(new List<T> { new T() })
    );

    protected void Push(T value)
    {
        if (stack.Value != null)
        {
            stack.Value.Push(value);
        }
    }

    protected void Pop()
    {
        if (stack.Value != null && stack.Value.Count > 1)
        {
            stack.Value.Pop();
        }
    }

    public static T Current => stack.Value?.Peek() ?? new();

    public virtual void Dispose() => Pop();
}
