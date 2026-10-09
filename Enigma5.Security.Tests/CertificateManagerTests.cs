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
using Enigma5.App.Common.Utils;
using Enigma5.Crypto;
using Enigma5.Tests.Base;

namespace Enigma5.Security.Tests;

// These tests store a passphrase in the kernel keyring of the test run. The entry gets a name of its own,
// so a node that runs on the same machine, with its own entry, is not touched.
public sealed class CertificateManagerTests : IDisposable
{
    private static readonly byte[] Text = [1, 2, 3, 4];

    private readonly List<IDisposable> _disposables = [];

    static CertificateManagerTests()
    {
        // The manager names the keyring entry when its class is first used; the test name must be set after that.
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(CertificateManager).TypeHandle);
        SealProvider.SetMasterPassphraseName($"enigma5key-test-{Guid.NewGuid()}");
    }

    public void Dispose()
    {
        SealProvider.SearchMasterPassphrase();
        SealProvider.RemoveMasterPassphrase();
        _disposables.ForEach(item => item.Dispose());
    }

    private CertificateManager Manager(KeyFiles files)
    {
        var manager = new CertificateManager(
            new SimpleSingleThreadRunner(),
            files.Configuration,
            new DummyPassphraseProvider(),
            new FileKeyReader(files.Configuration, new CapturingLogger<FileKeyReader>()),
            new CapturingLogger<CertificateManager>());
        _disposables.Add(manager);
        _disposables.Add(files);
        return manager;
    }

    private static byte[] Bytes(string passphrase) => Encoding.UTF8.GetBytes(passphrase);

    #region A key without passphrase

    [Fact]
    public async Task A_plain_key_can_sign_without_any_passphrase()
    {
        var manager = Manager(KeyFiles.For(TestKeys.PrivateKey3));

        Assert.True(await manager.CanSignAsync());
        using var signer = await manager.CreateSignerAsync();
        using var verifier = SealProvider.Factory.CreateVerifier(TestKeys.PublicKey3);
        Assert.True(verifier.Verify(signer.Sign(Text)!));
    }

    [Fact]
    public async Task A_plain_key_can_unseal_what_was_sealed_for_it()
    {
        var manager = Manager(KeyFiles.For(TestKeys.PrivateKey3));
        using var sealer = SealProvider.Factory.CreateSealer(TestKeys.PublicKey3);

        using var unsealer = await manager.CreateUnsealerAsync();

        Assert.Equal(Text, unsealer.Unseal(sealer.Seal(Text)!));
    }

    [Fact]
    public async Task The_keys_the_address_and_the_contact_data_come_from_the_key_files_and_the_settings()
    {
        var manager = Manager(KeyFiles.For(TestKeys.PrivateKey3));

        Assert.Equal(TestKeys.PublicKey3, await manager.GetPublicKeyAsync());
        Assert.Equal(TestKeys.PrivateKey3, await manager.GetPrivateKeyAsync());
        Assert.Equal(TestKeys.Address3, await manager.GetAddressAsync());
        var contact = await manager.GetExportedContactDataAsync();
        Assert.Equal(TestKeys.Address3, contact.Address);
        Assert.Equal(TestKeys.PublicKey3, contact.PublicKey);
        Assert.Equal("https://node.example.com", contact.Host);
        Assert.Equal("http://example.onion", contact.OnionService);
    }

    #endregion

    #region A key with passphrase

    [Fact]
    public async Task An_encrypted_key_cannot_sign_before_its_passphrase_is_given()
    {
        var manager = Manager(KeyFiles.For(TestKeys.PrivateKey1));

        Assert.False(await manager.CanSignAsync());
    }

    [Fact]
    public async Task An_encrypted_key_can_sign_after_its_passphrase_is_given_and_not_after_it_is_removed()
    {
        var manager = Manager(KeyFiles.For(TestKeys.PrivateKey1));

        Assert.True(await manager.CreateMasterPassphraseAsync(Bytes(TestKeys.Passphrase)));
        Assert.True(await manager.CanSignAsync());

        Assert.True(await manager.RemoveMasterPassphraseAsync());
        Assert.False(await manager.CanSignAsync());
    }

    [Fact]
    public async Task An_encrypted_key_can_unseal_after_its_passphrase_is_given()
    {
        var manager = Manager(KeyFiles.For(TestKeys.PrivateKey1));
        using var sealer = SealProvider.Factory.CreateSealer(TestKeys.PublicKey1);
        await manager.CreateMasterPassphraseAsync(Bytes(TestKeys.Passphrase));

        using var unsealer = await manager.CreateUnsealerAsync();

        Assert.Equal(Text, unsealer.Unseal(sealer.Seal(Text)!));
    }

    [Fact]
    public async Task A_wrong_passphrase_is_stored_but_the_key_cannot_sign()
    {
        var manager = Manager(KeyFiles.For(TestKeys.PrivateKey1));

        Assert.True(await manager.CreateMasterPassphraseAsync(Bytes("not the passphrase")));

        Assert.False(await manager.CanSignAsync());
    }

    [Fact]
    public async Task A_new_passphrase_replaces_the_one_given_before()
    {
        var manager = Manager(KeyFiles.For(TestKeys.PrivateKey1));

        await manager.CreateMasterPassphraseAsync(Bytes("not the passphrase"));
        await manager.CreateMasterPassphraseAsync(Bytes(TestKeys.Passphrase));

        Assert.True(await manager.CanSignAsync());
    }

    #endregion

    #region Setup

    [Fact]
    public async Task Setup_with_the_passphrase_of_an_existing_key_makes_the_key_usable()
    {
        var manager = Manager(KeyFiles.For(TestKeys.PrivateKey1));

        Assert.True(await manager.SetupAsync([.. TestKeys.Passphrase]));

        Assert.True(await manager.CanSignAsync());
    }

    // Setup does not try the key. Callers find out whether the passphrase was right by signing, as CanSignAsync does.
    [Fact]
    public async Task Setup_with_a_wrong_passphrase_succeeds_but_the_key_cannot_sign()
    {
        var manager = Manager(KeyFiles.For(TestKeys.PrivateKey1));

        Assert.True(await manager.SetupAsync([.. "not the passphrase"]));

        Assert.False(await manager.CanSignAsync());
    }

    [Fact]
    public async Task Setup_clears_the_passphrase_it_was_given()
    {
        var manager = Manager(KeyFiles.For(TestKeys.PrivateKey1));
        char[] passphrase = [.. TestKeys.Passphrase];

        await manager.SetupAsync(passphrase);

        Assert.All(passphrase, character => Assert.Equal('\0', character));
    }

    [Fact]
    public async Task Setup_without_passphrase_creates_a_plain_key_when_no_key_exists()
    {
        var files = KeyFiles.Empty();
        var manager = Manager(files);

        Assert.True(await manager.SetupAsync([]));

        Assert.DoesNotContain("ENCRYPTED", File.ReadAllText(files.PrivateKeyPath));
        Assert.Equal(512, SealProvider.GetPKeySize(File.ReadAllText(files.PublicKeyPath)));
        Assert.True(await manager.CanSignAsync());
    }

    [Fact]
    public async Task Setup_treats_an_empty_key_file_as_missing_and_writes_the_new_key_into_it()
    {
        var files = KeyFiles.Empty();
        File.WriteAllText(files.PrivateKeyPath, string.Empty);
        var manager = Manager(files);

        Assert.True(await manager.SetupAsync([]));

        Assert.True(new FileInfo(files.PrivateKeyPath).Length > 0);
        Assert.True(await manager.CanSignAsync());
    }

    #endregion

    #region Key files

    [Fact]
    public async Task GenerateKeysAsync_exports_a_missing_public_key_from_the_existing_private_key()
    {
        var files = KeyFiles.For(TestKeys.PrivateKey3);
        File.Delete(files.PublicKeyPath);
        var manager = Manager(files);

        Assert.True(await manager.GenerateKeysAsync([]));

        Assert.Equal(TestKeys.Address3, await manager.GetAddressAsync());
        Assert.Equal(TestKeys.PrivateKey3, File.ReadAllText(files.PrivateKeyPath));
    }

    [Fact]
    public async Task GenerateKeysAsync_leaves_existing_key_files_alone()
    {
        var files = KeyFiles.For(TestKeys.PrivateKey3);
        var manager = Manager(files);

        Assert.True(await manager.GenerateKeysAsync([]));

        Assert.Equal(TestKeys.PrivateKey3, File.ReadAllText(files.PrivateKeyPath));
        Assert.Equal(TestKeys.PublicKey3, File.ReadAllText(files.PublicKeyPath));
    }

    [Fact]
    public async Task Without_key_files_there_is_no_address_and_nothing_can_be_signed()
    {
        var manager = Manager(KeyFiles.Empty());

        Assert.Null(await manager.GetPublicKeyAsync());
        Assert.Equal(string.Empty, await manager.GetAddressAsync());
        Assert.False(await manager.CanSignAsync());
    }

    #endregion
}
