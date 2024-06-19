namespace Enigma5.App.Data;

public class PendingMessage
{
    public PendingMessage(string destination, string content, bool sent)
    {
        Destination = destination;
        Content = content;
        Sent = sent;    
    }

    public long Id { get; set; }

    public string Destination { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime DateReceived { get; set; } = DateTime.UtcNow;

    public bool Sent { get; set; }
}
