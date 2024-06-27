namespace Enigma5.App.Models;

public class SharedDataCreate
{
    public string? PublicKey { get; set; }

    public string? SignedData { get; set; }

    public int AccessCount { get; set; } = 1;

    public bool Valid => PublicKey != null && SignedData != null;
}
