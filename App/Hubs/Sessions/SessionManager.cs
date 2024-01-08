using Enigma5.App.Security;
using Enigma5.Crypto;
using Enigma5.Message;

namespace Enigma5.App.Hubs.Sessions;

public class SessionManager(ConnectionsMapper connectionsMapper, CertificateManager certificateManager)
{
    private readonly Mutex _mutex = new();

    private readonly Dictionary<string, string> _pending = [];

    private readonly HashSet<string> _authenticated = [];

    private readonly Dictionary<string, OnionParser> _parsers = [];

    private readonly ConnectionsMapper _connectionsMapper = connectionsMapper;

    private readonly CertificateManager _certificateManager = certificateManager;

    private bool AddPending(string connectionId, string token)
    {
        _mutex.WaitOne();
        var result = _pending.TryAdd(connectionId, token);
        _mutex.ReleaseMutex();

        return result;
    }

    private bool Authenticate(string connectionId)
    {
        _mutex.WaitOne();
        var result = _pending.Remove(connectionId) && _authenticated.Add(connectionId);
        _mutex.ReleaseMutex();

        return result;
    }

    public string? AddPending(string connectionId)
    {
        var tokenData = new byte[64];
        new Random().NextBytes(tokenData);
        var token = Convert.ToBase64String(tokenData);

        return AddPending(connectionId, token) ? token : null;
    }

    private bool AddParser(string connectionId)
    {
        var parser = OnionParser.Factory.Create(_certificateManager.PrivateKey, string.Empty);

        _mutex.WaitOne();
        var result = _parsers.TryAdd(connectionId, parser);
        _mutex.ReleaseMutex();

        return result;
    }

    private bool LogOut(string connectionId, out string? address)
    {
        _mutex.WaitOne();
        _pending.Remove(connectionId);
        _authenticated.Remove(connectionId);
        if (_parsers.TryGetValue(connectionId, out var parser))
        {
            parser.Dispose();
        }
        _parsers.Remove(connectionId);
        var result = _connectionsMapper.Remove(connectionId, out address);
        _mutex.ReleaseMutex();

        return result;
    }

    public bool Authenticate(string connectionId, string publicKey, string signature)
    {
        using var signatureVerifier = Envelope.Factory.CreateSignatureVerification(publicKey);
        var decodedSignature = Convert.FromBase64String(signature);

        if (signatureVerifier.Verify(decodedSignature) && Authenticate(connectionId))
        {
            var address = CertificateHelper.GetHexAddressFromPublicKey(publicKey);
            var added = _connectionsMapper.TryAdd(address, connectionId);

            if (!added)
            {
                return false;
            }

            return AddParser(connectionId);
        }

        return false;
    }

    public bool Remove(string connectionId, out string? address)
    => LogOut(connectionId, out address);

    public bool TryGetConnectionId(string address, out string? connectionId)
    => _connectionsMapper.TryGetConnectionId(address, out connectionId);

    public bool TryGetAddress(string connectionId, out string? address)
    => _connectionsMapper.TryGetAddress(connectionId, out address);

    public bool TryGetParser(string connectionId, out OnionParser? onionParser)
    {
        _mutex.WaitOne();
        var result = _parsers.TryGetValue(connectionId, out onionParser);
        _mutex.ReleaseMutex();

        return result;
    }
}
