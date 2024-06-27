namespace Enigma5.App.Common.Contracts.Hubs;

public interface IOnionParsingHub
{
    public int Size { get; set; }

    public string? Next { get; set; }

    public byte[]? Content { get; set; }
}
