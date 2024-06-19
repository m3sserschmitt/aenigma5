namespace Enigma5.App.Models;

public class ShareDataCreate
{
    public string? PublicKey { get; set; }

    public string? SignedData { get; set; }

    public bool Valid => PublicKey != null && SignedData != null;
}
