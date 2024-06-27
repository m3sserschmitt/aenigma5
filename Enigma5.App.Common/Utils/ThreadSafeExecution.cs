namespace Enigma5.App.Common.Utils;

public static class ThreadSafeExecution
{
    public delegate T Func<T, U>(out U a);

    public static T Execute<T>(Func<T> action, T defaultReturn, object locker)
    {
        T result = defaultReturn;

        lock (locker)
        {
            result = action();
        }

        return result;
    }

    public static T Execute<T, U>(Func<T, U> action, T defaultReturn, out U outParam, object locker)
    {
        T result = defaultReturn;

        lock(locker)
        {
            result = action(out outParam);
        }

        return result;
    }
}
