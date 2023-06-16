using Enigma5.Message;

namespace Enigma5.App.Hubs.Contracts;

public interface IOnionParserHub
{
    public int Size { get; set; }

    public string? Next { get; set; }

    public byte[]? Content { get; set; }
}
