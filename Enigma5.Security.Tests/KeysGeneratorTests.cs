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

using Enigma5.App.Common.Extensions;
using Enigma5.Crypto;
using Enigma5.Tests.Base;

namespace Enigma5.Security.Tests;

public class KeysGeneratorTests
{
    // 2048 bits keep these tests fast; the node itself creates 4096-bit keys.
    private const int KeySize = 2048;

    private readonly CapturingLogger<KeysGeneratorTests> _logger = new();

    [Fact]
    public async Task A_key_without_passphrase_is_generated_and_its_public_key_exported()
    {
        using var files = KeyFiles.Empty();

        Assert.True(await KeysGenerator.Generate(files.PrivateKeyPath, [], KeySize, _logger));
        Assert.True(await KeysGenerator.ExportPublicKey(files.PrivateKeyPath, files.PublicKeyPath, [], _logger));

        var privateKey = File.ReadAllText(files.PrivateKeyPath);
        var publicKey = File.ReadAllText(files.PublicKeyPath);
        Assert.True(privateKey.IsValidPrivateKey());
        Assert.DoesNotContain("ENCRYPTED", privateKey);
        Assert.True(publicKey.IsValidPublicKey());
        Assert.Equal(KeySize / 8, SealProvider.GetPKeySize(publicKey));
        Assert.Empty(_logger.Errors);
    }

    [Fact]
    public async Task A_key_with_passphrase_is_encrypted_and_can_be_used_with_that_passphrase()
    {
        using var files = KeyFiles.Empty();
        char[] passphrase = [.. "a test passphrase"];

        Assert.True(await KeysGenerator.Generate(files.PrivateKeyPath, passphrase, KeySize, _logger));
        Assert.True(await KeysGenerator.ExportPublicKey(files.PrivateKeyPath, files.PublicKeyPath, passphrase, _logger));

        Assert.Contains("ENCRYPTED", File.ReadAllText(files.PrivateKeyPath));
        using var signer = SealProvider.Factory.CreateSignerFromFile(files.PrivateKeyPath, [.. "a test passphrase\0"u8]);
        using var verifier = SealProvider.Factory.CreateVerifier(File.ReadAllText(files.PublicKeyPath));
        var signed = signer.Sign([1, 2, 3]);
        Assert.NotNull(signed);
        Assert.True(verifier.Verify(signed));
    }

    [Fact]
    public async Task Exporting_with_a_wrong_passphrase_fails_and_the_log_does_not_show_the_passphrase()
    {
        using var files = KeyFiles.For(TestKeys.PrivateKey1);
        File.Delete(files.PublicKeyPath);

        var exported = await KeysGenerator.ExportPublicKey(files.PrivateKeyPath, files.PublicKeyPath, [.. "wrong-passphrase-123"], _logger);

        Assert.False(exported);
        var error = Assert.Single(_logger.Errors);
        Assert.DoesNotContain("wrong-passphrase-123", error.Message);
        Assert.DoesNotContain(error.Properties.Values, value => $"{value}".Contains("wrong-passphrase-123"));
    }

    [Fact]
    public async Task Exporting_from_a_file_that_does_not_exist_fails()
    {
        using var files = KeyFiles.Empty();

        Assert.False(await KeysGenerator.ExportPublicKey(files.PrivateKeyPath, files.PublicKeyPath, [], _logger));
        Assert.Single(_logger.Errors);
    }
}
