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

    public DateTimeOffset DateReceived { get; set; } = DateTimeOffset.Now;

    public bool Sent { get; set; }
}
