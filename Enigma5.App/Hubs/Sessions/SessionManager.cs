/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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
using Enigma5.Crypto.Extensions;
using Enigma5.Crypto;
using Enigma5.App.Hubs.Sessions.Contracts;
using Enigma5.Security.Contracts;

namespace Enigma5.App.Hubs.Sessions;

public class SessionManager(
    ConnectionsMapper connectionsMapper,
    ICertificateManager certificateManager,
    ILogger<SessionManager> logger) : ISessionManager
{
    private readonly Dictionary<string, string> _pending = [];

    private readonly HashSet<string> _authenticated = [];

    private readonly ConnectionsMapper _connectionsMapper = connectionsMapper;

    private readonly ICertificateManager _certificateManager = certificateManager;

    private readonly ILogger _logger = logger;

    private readonly SimpleSingleThreadRunner _singleThreadExecutor = new();

    public IReadOnlyDictionary<string, string> Pending => _pending;

    public IReadOnlySet<string> Authenticated => _authenticated;

    public IReadOnlyConnectionsMapper ConnectionsMapper => _connectionsMapper;

    private bool AddPending(string connectionId, string token)
    {
        _pending.Remove(connectionId);
        return _pending.TryAdd(connectionId, token);
    }

    private bool Authenticate(string connectionId)
    {
        _authenticated.Remove(connectionId);
        return _pending.Remove(connectionId) && _authenticated.Add(connectionId);
    }

    public Task<string?> AddPendingAsync(string connectionId)
    => _singleThreadExecutor.RunAsync(() =>
        {
            var nonceData = new byte[Common.Constants.AuthTokenSize];
            new Random().NextBytes(nonceData);
            var nonce = Convert.ToBase64String(nonceData);
            return AddPending(connectionId, nonce) ? nonce : null;
        },
        _logger);

    private bool LogOut(string connectionId, out string? address)
    {
        _pending.Remove(connectionId);
        _authenticated.Remove(connectionId);
        return _connectionsMapper.Remove(connectionId, out address);
    }

    public Task<bool> AuthenticateAsync(string connectionId, string publicKey, string signature, string? impersonateServiceAddress)
    => _singleThreadExecutor.RunAsync(
            async () =>
            {
                using var signatureVerifier = SealProvider.Factory.CreateVerifier(publicKey);
                var decodedSignature = Convert.FromBase64String(signature);
                var nonce = decodedSignature.GetDataFromSignature(publicKey);

                if (nonce is null)
                {
                    return false;
                }

                var encodedNonce = Convert.ToBase64String(nonce);

                if (encodedNonce is null)
                {
                    return false;
                }

                var address = CertificateHelper.GetHexAddressFromPublicKey(publicKey);
                if (address == null)
                {
                    return false;
                }

                var impersonateAddressNull = string.IsNullOrWhiteSpace(impersonateServiceAddress);
                if (!_pending.TryGetValue(connectionId, out string? expectedNonce) ||
                    expectedNonce != encodedNonce ||
                    !signatureVerifier.Verify(decodedSignature) ||
                    !Authenticate(connectionId) ||
                    (!impersonateAddressNull && await _certificateManager.GetAddressAsync() != address)
                )
                {
                    return false;
                }

                address = !impersonateAddressNull ? impersonateServiceAddress : address;
                return _connectionsMapper.TryAdd(address!, connectionId);
            },
            _logger
        );

    public Task<string?> RemoveAsync(string connectionId)
    => _singleThreadExecutor.RunAsync(
        () => LogOut(connectionId, out var address) ? address : null,
        _logger
    );

    public Task<string?> TryGetConnectionIdAsync(string address)
    => _singleThreadExecutor.RunAsync(
        () => _connectionsMapper.TryGetConnectionId(address, out var connectionId) ? connectionId : null,
        _logger
    );

    public Task<string?> TryGetAddressAsync(string connectionId)
    => _singleThreadExecutor.RunAsync(
        () => _connectionsMapper.TryGetAddress(connectionId, out var address) ? address : null,
        _logger
    );
}
