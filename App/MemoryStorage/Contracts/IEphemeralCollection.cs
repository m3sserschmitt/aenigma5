namespace Enigma5.App.MemoryStorage.Contracts;

public interface IEphemeralCollection<TItem>
{
    void Add(TItem item);

    IEnumerable<TItem> Where(Func<TItem, bool> condition);

    void Cleanup(TimeSpan deadline);
}
