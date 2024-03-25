using Enigma5.App.Common.Utils;
using Enigma5.App.Security.Contracts;
using Enigma5.Crypto;
using Enigma5.Message;

namespace Enigma5.App.Hubs.Sessions;

public class SessionManager(ConnectionsMapper connectionsMapper, ICertificateManager certificateManager)
{
    private const int TOKEN_SIZE = 64;

    private readonly object _locker = new();

    private readonly Dictionary<string, string> _pending = [];

    private readonly HashSet<string> _authenticated = [];

    private readonly Dictionary<string, OnionParser> _parsers = [];

    private readonly ConnectionsMapper _connectionsMapper = connectionsMapper;

    private readonly ICertificateManager _certificateManager = certificateManager;

    private bool AddPending(string connectionId, string token)
    => _pending.TryAdd(connectionId, token);

    private bool Authenticate(string connectionId)
    => _pending.Remove(connectionId) && _authenticated.Add(connectionId);

    public string? AddPending(string connectionId)
    {
        var tokenData = new byte[TOKEN_SIZE];
        new Random().NextBytes(tokenData);
        var token = Convert.ToBase64String(tokenData);

        return ThreadSafeExecution.Execute(
            () => AddPending(connectionId, token) ? token : null,
            null,
            _locker);
    }

    private bool AddParser(string connectionId)
    {
        var parser = OnionParser.Factory.Create(_certificateManager.PrivateKey, string.Empty);

        return _parsers.TryAdd(connectionId, parser);
    }

    private bool LogOut(string connectionId, out string? address)
    {
        _pending.Remove(connectionId);
        _authenticated.Remove(connectionId);
        if (_parsers.TryGetValue(connectionId, out var parser))
        {
            parser.Dispose();
        }
        _parsers.Remove(connectionId);

        return _connectionsMapper.Remove(connectionId, out address);
    }

    public bool Authenticate(string connectionId, string publicKey, string signature)
    {
        using var signatureVerifier = Envelope.Factory.CreateSignatureVerification(publicKey);
        var decodedSignature = Convert.FromBase64String(signature);

        return ThreadSafeExecution.Execute(
            () =>
            {
                if (!signatureVerifier.Verify(decodedSignature) || !Authenticate(connectionId))
                {
                    return false;
                }

                var address = CertificateHelper.GetHexAddressFromPublicKey(publicKey);
                var added = _connectionsMapper.TryAdd(address, connectionId);

                return added && AddParser(connectionId);
            },
            false,
            _locker
        );
    }

    public bool Remove(string connectionId, out string? address)
    => ThreadSafeExecution.Execute(
        (out string? addr) => LogOut(connectionId, out addr),
        false,
        out address,
        _locker
    );

    public bool TryGetConnectionId(string address, out string? connectionId)
    => ThreadSafeExecution.Execute(
        (out string? connId) => _connectionsMapper.TryGetConnectionId(address, out connId),
        false,
        out connectionId,
        _locker
    );

    public bool TryGetAddress(string connectionId, out string? address)
    => ThreadSafeExecution.Execute(
        (out string? addr) => _connectionsMapper.TryGetAddress(connectionId, out addr),
        false,
        out address,
        _locker
    );

    public bool TryGetParser(string connectionId, out OnionParser? onionParser)
    => ThreadSafeExecution.Execute(
        (out OnionParser? parser) => _parsers.TryGetValue(connectionId, out parser),
        false,
        out onionParser,
        _locker
    );
}
