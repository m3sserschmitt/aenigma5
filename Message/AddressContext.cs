namespace Message;

public sealed class AddressContext : IDisposable
{
    public int AddressSize { get; private set; }

    private static Stack<AddressContext> stack = new();

    static AddressContext()
    {
        stack.Push(new AddressContext());
    }

    private AddressContext()
    {
        AddressSize = DefaultAddressSize;
    }

    public AddressContext(int addressSize)
    {
        AddressSize = addressSize;
        stack.Push(this);
    }

    public static readonly int DefaultAddressSize = 32;

    public static AddressContext Current => stack.Peek();

    public void Dispose()
    {
        if(stack.Count > 1)
        {
            stack.Pop();
        }
    }
}
