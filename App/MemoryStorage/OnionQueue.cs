using Enigma5.App.MemoryStorage.Contracts;

namespace Enigma5.App.MemoryStorage;

public class OnionQueue : IEphemeralCollection<OnionQueueItem>
{
    private readonly Mutex mutex = new();

    private readonly List<OnionQueueItem> queue = new();

    public void Add(OnionQueueItem item)
    {
        mutex.WaitOne();
        queue.Add(item);
        mutex.ReleaseMutex();
    }

    public IEnumerable<OnionQueueItem> Where(Func<OnionQueueItem, bool> condition)
    {
        mutex.WaitOne();
        var onions = queue.Where(condition);
        mutex.ReleaseMutex();

        return onions;
    }

    public void Cleanup(TimeSpan deadline)
    {
        mutex.WaitOne();
        queue.RemoveAll(item => (DateTime.Now - item.DateReceived) > deadline);
        mutex.ReleaseMutex();
    }
}
