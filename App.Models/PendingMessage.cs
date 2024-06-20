namespace Enigma5.App.Models;

public class PendingMessage
{
    public string? Destination { get; set; }

    public string? Content { get; set; }

    public DateTimeOffset DateReceived { get; set; }
}
