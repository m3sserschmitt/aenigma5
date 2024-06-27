namespace Enigma5.App.Models;

public class AuthenticationRequest
{
    public string? PublicKey { get; set; }

    public string? Signature { get; set; }

    public bool SyncMessagesOnSuccess { get; set; }

    public bool UpdateNetworkGraph { get; set; }
}
