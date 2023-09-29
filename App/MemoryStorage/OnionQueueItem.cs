namespace Enigma5.App.MemoryStorage;

public class OnionQueueItem {

    public byte[] Content { get; set; } = new byte[] { 0 };

    public string Destination { get; set; } = string.Empty;

    public DateTime DateReceived { get; set; } = DateTime.Now;
}
