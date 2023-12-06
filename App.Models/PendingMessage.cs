namespace Enigma5.App.Models;

public class PendingMessage
{
    public string? Destination { get; set; }

    public string? Content { get; set; }

    public DateTime DateReceived { get; set; }
}
