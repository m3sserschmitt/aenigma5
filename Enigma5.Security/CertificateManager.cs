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
using Enigma5.App.Common.Enums;
using Enigma5.App.Common.Extensions;
using Enigma5.App.Common.Utils;
using Enigma5.App.Models;
using Enigma5.Crypto;
using Enigma5.Crypto.Contracts;
using Enigma5.Security.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Enigma5.Security;

public sealed class CertificateManager(
    SimpleSingleThreadRunner simpleSingleThreadRunner,
    IConfiguration configuration,
    IPassphraseProvider passphraseProvider,
    IKeyReader keysProvider,
    ILogger<CertificateManager> logger) : ICertificateManager, IDisposable
{
    private bool _disposed;

    private readonly SimpleSingleThreadRunner _simpleSingleThreadRunner = simpleSingleThreadRunner;

    private readonly IKeyReader _keysProvider = keysProvider;

    private readonly IPassphraseProvider _passphraseProvider = passphraseProvider;

    private readonly IConfiguration _configuration = configuration;

    private readonly ILogger _logger = logger;

    private const string KERNEL_KEY_DESCRIPTION = "enigma5key: Key used for cryptographic operations.";

    static CertificateManager()
    {
        SealProvider.SetMasterPassphraseName(KERNEL_KEY_DESCRIPTION);
    }

    ~CertificateManager()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {

            }
            _simpleSingleThreadRunner.Dispose();
            _disposed = true;
        }
    }

    private int SearchMasterPassphrase() => _configuration.GetPassphrasePersistence() switch
    {
        PassphrasePersistence.Persistent => SealProvider.SearchPersistentMasterPassphrase(),
        PassphrasePersistence.Ephemeral => SealProvider.SearchMasterPassphrase(),
        _ => 0
    };
    
    public Task<bool> CreateMasterPassphraseAsync(byte[] passphrase)
    => _simpleSingleThreadRunner.RunAsync(() =>
    {
        SearchMasterPassphrase();
        SealProvider.RemoveMasterPassphrase();
        return SealProvider.CreatePersistentMasterPassphrase(passphrase) > 0;
    }, _logger);

    public Task<bool> RemoveMasterPassphraseAsync() => _simpleSingleThreadRunner.RunAsync(() =>
    {
        SearchMasterPassphrase();
        return SealProvider.RemoveMasterPassphrase();
    }, _logger);

    public Task<IEnvelopeUnsealer> CreateUnsealerAsync()
    => _simpleSingleThreadRunner.RunAsync(() =>
    {
        SearchMasterPassphrase();
        return SealProvider.Factory.CreateUnsealerFromFile(_keysProvider.PrivateKeyPath ?? string.Empty);
    }, _logger);

    public Task<IEnvelopeSigner> CreateSignerAsync()
    => _simpleSingleThreadRunner.RunAsync(() =>
    {
        SearchMasterPassphrase();
        return SealProvider.Factory.CreateSignerFromFile(_keysProvider.PrivateKeyPath ?? string.Empty);
    }, _logger);

    public async Task<bool> SetupAsync(char[] passphrase)
    {
        try
        {
            var passphraseChars = passphrase.Length == 0 ? await _passphraseProvider.ProvidePassphraseAsync() : passphrase;
            if (passphraseChars == null)
            {
                return false;
            }
            var passphraseBytes = Encoding.UTF8.GetBytes(passphraseChars);
            try
            {
                return passphraseChars.Length == 0 ? await GenerateKeysAsync(passphraseChars)
                : await GenerateKeysAsync(passphraseChars) && await CreateMasterPassphraseAsync(passphraseBytes);
            }
            finally
            {
                Array.Clear(passphraseBytes);
                Array.Clear(passphraseChars);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception encountered while setting up master passphrase.");
            return false;
        }
    }

    public async Task<bool> GenerateKeysAsync(char[] passphrase)
    {
        var publicKeyPath = _keysProvider.PublicKeyPath;
        var privateKeyPath = _keysProvider.PrivateKeyPath;

        if (string.IsNullOrWhiteSpace(publicKeyPath) || string.IsNullOrWhiteSpace(privateKeyPath))
        {
            return false;
        }

        var privateKeyFileInfo = new FileInfo(privateKeyPath);
        if (!privateKeyFileInfo.Exists || privateKeyFileInfo.Length == 0)
        {
            return await KeysGenerator.Generate(privateKeyPath, passphrase) &&
            await KeysGenerator.ExportPublicKey(privateKeyPath, publicKeyPath, passphrase);
        }
        else
        {
            var publicKeyFileInfo = new FileInfo(publicKeyPath);
            if (!publicKeyFileInfo.Exists || publicKeyFileInfo.Length == 0)
            {
                return await KeysGenerator.ExportPublicKey(privateKeyPath, publicKeyPath, passphrase);
            }
        }
        return true;
    }

    public Task<string?> GetPublicKeyAsync() => _keysProvider.ReadPublicKeyAsync();

    public Task<string?> GetPrivateKeyAsync() => _keysProvider.ReadPrivateKeyAsync();

    public async Task<ExportedContactDataDto> GetExportedContactDataAsync() => new()
    {
        Host = _configuration.GetHostname(),
        OnionService = _configuration.GetOnionService(),
        Address = await GetAddressAsync(),
        PublicKey = await GetPublicKeyAsync()
    };

    public async Task<string?> GetAddressAsync()
    => CertificateHelper.GetHexAddressFromPublicKey(await GetPublicKeyAsync());
}
