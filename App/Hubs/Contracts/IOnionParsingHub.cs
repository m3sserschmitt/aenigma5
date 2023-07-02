namespace Enigma5.App.Hubs.Contracts;

public interface IOnionParsingHub
{
    public int Size { get; set; }

    public string? Next { get; set; }

    public byte[]? Content { get; set; }
}
