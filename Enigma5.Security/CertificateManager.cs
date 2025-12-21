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

using System.Text;
using Enigma5.App.Common.Utils;
using Enigma5.Crypto;
using Enigma5.Crypto.Contracts;
using Enigma5.Security.Contracts;

namespace Enigma5.Security;

public sealed class CertificateManager(IPassphraseProvider passphraseProvider, IKeyReader keysProvider) : ICertificateManager, IDisposable
{
    private readonly SimpleSingleThreadRunner _simpleSingleThreadRunner = new();

    private readonly IKeyReader _keysProvider = keysProvider;

    private readonly IPassphraseProvider _passphraseProvider = passphraseProvider;

    private const string KERNEL_KEY_DESCRIPTION = "enigma5key: Key used for cryptographic operations.";

    private readonly object _locker = new();

    private string? _publicKey = null;

    public string PublicKey => ThreadSafeExecution.Execute(() => _publicKey ??= _keysProvider.ReadPublicKey(), string.Empty, _locker);

    public string PrivateKey => ThreadSafeExecution.Execute(() => _keysProvider.ReadPrivateKey(), string.Empty, _locker);

    public string Address { get => CertificateHelper.GetHexAddressFromPublicKey(PublicKey); }

    static CertificateManager()
    {
        SealProvider.SetMasterPassphraseName(KERNEL_KEY_DESCRIPTION);
    }

    public bool GenerateKeys(char[] passphrase)
    {
        if (!File.Exists(_keysProvider.PrivateKeyPath))
        {
            return KeysGenerator.Generate(_keysProvider.PrivateKeyPath, passphrase) &&
            KeysGenerator.ExportPublicKey(_keysProvider.PrivateKeyPath, _keysProvider.PublicKeyPath, passphrase);
        }
        else if (!File.Exists(_keysProvider.PublicKeyPath))
        {
            return KeysGenerator.ExportPublicKey(_keysProvider.PrivateKeyPath, _keysProvider.PublicKeyPath, passphrase);
        }
        return true;
    }

    public bool SetMasterPassphrase(byte[] passphrase) =>
    _simpleSingleThreadRunner.RunAsync(() => SealProvider.CreateMasterPassphrase(passphrase) > 0).GetAwaiter().GetResult();

    public bool Setup(char[]? passphrase)
    {
        var passphraseChars = passphrase ?? _passphraseProvider.ProvidePassphrase();
        var passphraseBytes = Encoding.UTF8.GetBytes(passphraseChars);
        var ok = passphraseChars.Length != 0 && GenerateKeys(passphraseChars) && SetMasterPassphrase(passphraseBytes);
        Array.Clear(passphraseBytes);
        Array.Clear(passphraseChars);
        return ok;
    }

    public void Dispose() { _simpleSingleThreadRunner.Dispose(); }

    public IEnvelopeUnsealer CreateUnsealer()
    => _simpleSingleThreadRunner.RunAsync(() => SealProvider.Factory.CreateUnsealer(PrivateKey)).GetAwaiter().GetResult();

    public IEnvelopeSigner CreateSigner()
    => _simpleSingleThreadRunner.RunAsync(() => SealProvider.Factory.CreateSigner(PrivateKey)).GetAwaiter().GetResult();
}
