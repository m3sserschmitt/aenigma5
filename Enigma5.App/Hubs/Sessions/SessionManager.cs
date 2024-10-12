/*
    Aenigma - Onion Routing based messaging application
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

using Enigma5.App.Common.Utils;
using Enigma5.App.Models.Extensions;
using Enigma5.Security.Contracts;
using Enigma5.Crypto;
using Enigma5.Structures;

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
                var token = decodedSignature.GetDataFromSignature();

                if (token is null)
                {
                    return false;
                }

                var encodedToken = Convert.ToBase64String(token);

                if (encodedToken is null)
                {
                    return false;
                }

                if (!_pending.TryGetValue(connectionId, out string? t) ||
                    t != encodedToken ||
                    !signatureVerifier.Verify(decodedSignature) ||
                    !Authenticate(connectionId)
                )
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
