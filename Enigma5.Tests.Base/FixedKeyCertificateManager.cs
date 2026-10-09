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

using System.Text;
using Enigma5.App.Models;
using Enigma5.Crypto;
using Enigma5.Crypto.Contracts;
using Enigma5.Security.Contracts;

namespace Enigma5.Tests.Base;

// A certificate manager for one of the fixed test keys. It does real signing and unsealing.
// With an encrypted key (TestKeys.PrivateKey1 or PrivateKey2), Locked makes it behave like a node
// whose passphrase is not available: signing and unsealing then fail.
public sealed class FixedKeyCertificateManager(string privateKey, string? passphrase = null) : ICertificateManager
{
    private readonly string _publicKey = TestKeys.PublicKeyOf(privateKey);

    public static FixedKeyCertificateManager Key1() => new(TestKeys.PrivateKey1, TestKeys.Passphrase);

    public static FixedKeyCertificateManager Key2() => new(TestKeys.PrivateKey2, TestKeys.Passphrase);

    public static FixedKeyCertificateManager Key3() => new(TestKeys.PrivateKey3);

    public bool Locked { get; set; }

    public string Address => CertificateHelper.GetHexAddressFromPublicKey(_publicKey);

    private byte[]? Passphrase => Locked || string.IsNullOrEmpty(passphrase) ? null : Encoding.UTF8.GetBytes(passphrase + "\0");

    public Task<string?> GetPublicKeyAsync() => Task.FromResult<string?>(_publicKey);

    public Task<string?> GetPrivateKeyAsync() => Task.FromResult<string?>(privateKey);

    public Task<string?> GetAddressAsync() => Task.FromResult<string?>(Address);

    public Task<bool> GenerateKeysAsync(char[] passphrase) => Task.FromResult(false);

    public Task<bool> CreateMasterPassphraseAsync(byte[] passphrase)
    {
        Locked = false;
        return Task.FromResult(true);
    }

    public Task<bool> RemoveMasterPassphraseAsync()
    {
        Locked = true;
        return Task.FromResult(true);
    }

    public Task<bool> SetupAsync(char[] passphrase) => CanSignAsync();

    public Task<IEnvelopeUnsealer> CreateUnsealerAsync()
    => Task.FromResult(SealProvider.Factory.CreateUnsealer(privateKey, _publicKey, Passphrase));

    public Task<IEnvelopeSigner> CreateSignerAsync()
    => Task.FromResult(SealProvider.Factory.CreateSigner(privateKey, Passphrase));

    public async Task<bool> CanSignAsync()
    {
        using var signer = await CreateSignerAsync();
        return signer.Sign([0]) is not null;
    }

    public Task<ExportedContactDataDto> GetExportedContactDataAsync() => Task.FromResult(new ExportedContactDataDto());
}
