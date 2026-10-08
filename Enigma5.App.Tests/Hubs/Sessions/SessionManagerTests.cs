/*
    Aenigma - Federated messaging system
    Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

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
using Enigma5.App.Hubs.Sessions;
using Enigma5.Tests.Base;

namespace Enigma5.App.Tests.Hubs.Sessions;

public sealed class SessionManagerTests : IDisposable
{
    private const string Connection1 = "connection-1";

    private const string Connection2 = "connection-2";

    private readonly SimpleSingleThreadRunner _runner = new();

    // The node itself has key 3. Keys 1 and 2 belong to clients.
    private readonly SessionManager _sessions;

    public SessionManagerTests()
    {
        _sessions = new SessionManager(new ConnectionsMapper(), FixedKeyCertificateManager.Key3(), _runner, new CapturingLogger<SessionManager>());
    }

    public void Dispose() => _runner.Dispose();

    private async Task<bool> SignIn(string connectionId, string privateKey, string? impersonate = null)
    {
        var challenge = await _sessions.AddPendingAsync(connectionId);
        return await _sessions.AuthenticateAsync(connectionId, TestKeys.PublicKeyOf(privateKey), TestSignatures.SignChallenge(privateKey, challenge!), impersonate);
    }

    #region Challenge

    [Fact]
    public async Task A_challenge_is_64_random_bytes_in_base64()
    {
        var first = await _sessions.AddPendingAsync(Connection1);
        var second = await _sessions.AddPendingAsync(Connection2);

        Assert.Equal(64, Convert.FromBase64String(first!).Length);
        Assert.NotEqual(first, second);
        Assert.Equal(first, _sessions.Pending[Connection1]);
    }

    [Fact]
    public async Task A_new_challenge_replaces_the_one_before()
    {
        var first = await _sessions.AddPendingAsync(Connection1);
        var second = await _sessions.AddPendingAsync(Connection1);

        Assert.NotEqual(first, second);
        Assert.False(await _sessions.AuthenticateAsync(Connection1, TestKeys.PublicKey1, TestSignatures.SignChallenge(TestKeys.PrivateKey1, first!), null));
    }

    #endregion

    #region Sign-in

    [Fact]
    public async Task A_connection_that_signs_its_challenge_is_signed_in_under_the_address_of_its_key()
    {
        Assert.True(await SignIn(Connection1, TestKeys.PrivateKey1));

        Assert.Equal(TestKeys.Address1, await _sessions.TryGetAddressAsync(Connection1));
        Assert.Equal(Connection1, await _sessions.TryGetConnectionIdAsync(TestKeys.Address1));
        Assert.Contains(Connection1, _sessions.Authenticated);
        Assert.Empty(_sessions.Pending);
    }

    [Fact]
    public async Task A_challenge_can_be_used_once()
    {
        var challenge = await _sessions.AddPendingAsync(Connection1);
        var signature = TestSignatures.SignChallenge(TestKeys.PrivateKey1, challenge!);

        Assert.True(await _sessions.AuthenticateAsync(Connection1, TestKeys.PublicKey1, signature, null));
        Assert.False(await _sessions.AuthenticateAsync(Connection1, TestKeys.PublicKey1, signature, null));

        Assert.Equal(TestKeys.Address1, await _sessions.TryGetAddressAsync(Connection1));
    }

    [Fact]
    public async Task A_failed_attempt_also_uses_the_challenge_up()
    {
        var challenge = await _sessions.AddPendingAsync(Connection1);

        Assert.False(await _sessions.AuthenticateAsync(Connection1, TestKeys.PublicKey1, TestSignatures.SignChallenge(TestKeys.PrivateKey2, challenge!), null));
        Assert.False(await _sessions.AuthenticateAsync(Connection1, TestKeys.PublicKey1, TestSignatures.SignChallenge(TestKeys.PrivateKey1, challenge!), null));

        Assert.Null(await _sessions.TryGetAddressAsync(Connection1));
    }

    [Fact]
    public async Task A_signature_made_with_another_key_is_refused()
    {
        var challenge = await _sessions.AddPendingAsync(Connection1);

        Assert.False(await _sessions.AuthenticateAsync(Connection1, TestKeys.PublicKey1, TestSignatures.SignChallenge(TestKeys.PrivateKey2, challenge!), null));
    }

    [Fact]
    public async Task A_signature_over_another_challenge_is_refused()
    {
        await _sessions.AddPendingAsync(Connection1);
        var other = await _sessions.AddPendingAsync(Connection2);

        Assert.False(await _sessions.AuthenticateAsync(Connection1, TestKeys.PublicKey1, TestSignatures.SignChallenge(TestKeys.PrivateKey1, other!), null));
    }

    [Fact]
    public async Task A_connection_without_challenge_is_refused()
    {
        var challenge = await _sessions.AddPendingAsync(Connection2);

        Assert.False(await _sessions.AuthenticateAsync(Connection1, TestKeys.PublicKey1, TestSignatures.SignChallenge(TestKeys.PrivateKey1, challenge!), null));
    }

    [Fact]
    public async Task Text_that_is_not_a_public_key_is_refused()
    {
        var challenge = await _sessions.AddPendingAsync(Connection1);

        Assert.False(await _sessions.AuthenticateAsync(Connection1, "not a key", TestSignatures.SignChallenge(TestKeys.PrivateKey1, challenge!), null));
        // The challenge was not used up, because the request was refused before it was looked at.
        Assert.Single(_sessions.Pending);
    }

    #endregion

    #region One session for each address

    [Fact]
    public async Task A_second_sign_in_with_the_same_key_takes_the_session_over()
    {
        Assert.True(await SignIn(Connection1, TestKeys.PrivateKey1));

        Assert.True(await SignIn(Connection2, TestKeys.PrivateKey1));

        Assert.Equal(Connection2, await _sessions.TryGetConnectionIdAsync(TestKeys.Address1));
        Assert.Equal(TestKeys.Address1, await _sessions.TryGetAddressAsync(Connection2));
        Assert.Null(await _sessions.TryGetAddressAsync(Connection1));
    }

    [Fact]
    public async Task Two_keys_have_two_sessions()
    {
        Assert.True(await SignIn(Connection1, TestKeys.PrivateKey1));
        Assert.True(await SignIn(Connection2, TestKeys.PrivateKey2));

        Assert.Equal(Connection1, await _sessions.TryGetConnectionIdAsync(TestKeys.Address1));
        Assert.Equal(Connection2, await _sessions.TryGetConnectionIdAsync(TestKeys.Address2));
    }

    [Fact]
    public async Task Asking_for_a_new_challenge_ends_the_session_of_the_connection()
    {
        await SignIn(Connection1, TestKeys.PrivateKey1);

        await _sessions.AddPendingAsync(Connection1);

        Assert.Null(await _sessions.TryGetAddressAsync(Connection1));
        Assert.Null(await _sessions.TryGetConnectionIdAsync(TestKeys.Address1));
        Assert.DoesNotContain(Connection1, _sessions.Authenticated);
    }

    #endregion

    #region Signing in for another address

    [Fact]
    public async Task The_key_of_the_node_may_sign_in_for_another_address()
    {
        Assert.True(await SignIn(Connection1, TestKeys.PrivateKey3, impersonate: TestKeys.Address1));

        Assert.Equal(TestKeys.Address1, await _sessions.TryGetAddressAsync(Connection1));
        Assert.Null(await _sessions.TryGetConnectionIdAsync(TestKeys.Address3));
    }

    [Fact]
    public async Task Any_other_key_may_not_sign_in_for_another_address()
    {
        Assert.False(await SignIn(Connection1, TestKeys.PrivateKey1, impersonate: TestKeys.Address2));

        Assert.Null(await _sessions.TryGetAddressAsync(Connection1));
        // Refused before the challenge was looked at.
        Assert.Single(_sessions.Pending);
    }

    [Fact]
    public async Task The_other_address_must_be_a_valid_address()
    {
        Assert.False(await SignIn(Connection1, TestKeys.PrivateKey3, impersonate: "not-an-address"));
    }

    #endregion

    #region Removal

    [Fact]
    public async Task Removing_a_connection_ends_its_session_and_gives_its_address()
    {
        await SignIn(Connection1, TestKeys.PrivateKey1);

        Assert.Equal(TestKeys.Address1, await _sessions.RemoveAsync(Connection1));

        Assert.Null(await _sessions.TryGetConnectionIdAsync(TestKeys.Address1));
        Assert.DoesNotContain(Connection1, _sessions.Authenticated);
    }

    [Fact]
    public async Task Removing_a_connection_that_never_signed_in_gives_no_address_and_drops_its_challenge()
    {
        await _sessions.AddPendingAsync(Connection1);

        Assert.Null(await _sessions.RemoveAsync(Connection1));

        Assert.Empty(_sessions.Pending);
    }

    [Fact]
    public async Task Removing_a_connection_whose_session_was_taken_over_gives_no_address_and_leaves_the_new_session()
    {
        await SignIn(Connection1, TestKeys.PrivateKey1);
        await SignIn(Connection2, TestKeys.PrivateKey1);

        Assert.Null(await _sessions.RemoveAsync(Connection1));

        Assert.Equal(Connection2, await _sessions.TryGetConnectionIdAsync(TestKeys.Address1));
    }

    #endregion
}
