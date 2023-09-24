namespace Enigma5.App.Hubs.Queues;

public class OnionQueue
{
    private readonly Mutex mutex = new();

    private readonly List<OnionQueueItem> queue = new();

    public void Enqueue(OnionQueueItem item)
    {
        mutex.WaitOne();
        queue.Add(item);
        mutex.ReleaseMutex();
    }

    public IEnumerable<OnionQueueItem> Get(string address)
    {
        mutex.WaitOne();
        var onions = queue.Where(item => item.Destination == address);
        mutex.ReleaseMutex();

        return onions;
    }

    public void Cleanup()
    {
        mutex.WaitOne();
        queue.RemoveAll(item => (DateTime.Now - item.DateReceived).TotalDays > 1);
        mutex.ReleaseMutex();
    }
}
