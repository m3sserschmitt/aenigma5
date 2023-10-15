namespace Enigma5.App.Data;

public class PendingMessage
{
    public long Id { get; set; }

    public string Destination { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime DateReceived { get; set; }

    public bool Sent { get; set; }
}
