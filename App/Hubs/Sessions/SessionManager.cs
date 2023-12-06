using Enigma5.Crypto;

namespace Enigma5.App.Hubs.Sessions;

public class SessionManager
{
    private readonly Dictionary<string, string> pending = new();

    private readonly HashSet<string> authenticated = new();

    private readonly ConnectionsMapper connectionsMapper;

    public SessionManager(ConnectionsMapper connectionsMapper)
    {
        this.connectionsMapper = connectionsMapper;
    }

    private bool AddPending(string connectionId, string token)
     => pending.TryAdd(connectionId, token);

    private bool Authenticate(string connectionId)
    => pending.Remove(connectionId) && authenticated.Add(connectionId);

    public string? AddPending(string connectionId)
    {
        var tokenData = new byte[64];
        new Random().NextBytes(tokenData);
        var token = Convert.ToBase64String(tokenData);

        return AddPending(connectionId, token) ? token : null;
    }

    public bool Authenticate(string connectionId, string publicKey, string signature)
    {
        using var signatureVerifier = Envelope.Factory.CreateSignatureVerification(publicKey);
        var decodedSignature = Convert.FromBase64String(signature);

        if (signatureVerifier.Verify(decodedSignature) && Authenticate(connectionId))
        {
            var address = CertificateHelper.GetHexAddressFromPublicKey(publicKey);
            return connectionsMapper.TryAdd(address, connectionId);
        }

        return false;
    }

    public string? Remove(string connectionId)
    {
        pending.Remove(connectionId);
        authenticated.Remove(connectionId);
        return connectionsMapper.Remove(connectionId);
    }

    public bool TryGetConnectionId(string address, out string? connectionId)
    => connectionsMapper.TryGetConnectionId(address, out connectionId);

    public bool TryGetAddress(string connectionId, out string? address)
    => connectionsMapper.TryGetAddress(connectionId, out address);
}
