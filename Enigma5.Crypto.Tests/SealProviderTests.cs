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
using Enigma5.Crypto.Extensions;
using Enigma5.Crypto.Tests.TestData;
using Enigma5.Tests.Base;

namespace Enigma5.Crypto.Tests;

public class SealProviderTests
{
    private static readonly byte[] Plaintext = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x23, 0x56, 0x11];

    // The native library reads a passphrase as text that ends with a zero byte. A key without passphrase gets none.
    private static byte[]? Passphrase(string? passphrase)
    => string.IsNullOrEmpty(passphrase) ? null : Encoding.UTF8.GetBytes(passphrase + "\0");

    #region Stored data

    [Theory]
    [ClassData(typeof(UnsealerData))]
    public void Unseal_reads_a_stored_ciphertext(byte[] ciphertext, string key, string passphrase, byte[]? expectedPlaintext)
    {
        using var unsealer = SealProvider.Factory.CreateUnsealer(key, TestKeys.PublicKeyOf(key), Passphrase(passphrase));

        Assert.Equal(expectedPlaintext, unsealer.Unseal(ciphertext));
    }

    [Theory]
    [ClassData(typeof(VerifierData))]
    public void Verify_checks_a_stored_signature(byte[] signedData, string key, bool expected)
    {
        using var verifier = SealProvider.Factory.CreateVerifier(key);

        Assert.Equal(expected, verifier.Verify(signedData));
    }

    [Theory]
    [ClassData(typeof(OnionUnsealerData))]
    public void UnsealOnion_removes_one_layer_of_a_stored_onion(string onion, string key, string passphrase, string? expectedNext, byte[]? expectedContent)
    {
        using var unsealer = SealProvider.Factory.CreateUnsealer(key, TestKeys.PublicKeyOf(key), Passphrase(passphrase));
        string? next = null;
        byte[]? content = null;

        var result = unsealer.UnsealOnion(onion, ref next, ref content);

        Assert.Equal(expectedNext is not null, result);
        Assert.Equal(expectedNext, next);
        Assert.Equal(expectedContent, content);
    }

    #endregion

    #region Sealing and signing

    [Theory]
    [ClassData(typeof(SealerData))]
    public void Seal_gives_a_ciphertext_of_the_expected_length(byte[] plaintext, string key, int expectedLength)
    {
        using var sealer = SealProvider.Factory.CreateSealer(key);

        Assert.Equal(expectedLength, sealer.Seal(plaintext)?.Length);
    }

    [Fact]
    public void A_sealed_text_is_read_again_with_the_private_key()
    {
        using var sealer = SealProvider.Factory.CreateSealer(TestKeys.PublicKey1);
        using var unsealer = SealProvider.Factory.CreateUnsealer(TestKeys.PrivateKey1, TestKeys.PublicKey1, Passphrase(TestKeys.Passphrase));

        var ciphertext = sealer.Seal(Plaintext);

        Assert.NotNull(ciphertext);
        Assert.Equal(Plaintext, unsealer.Unseal(ciphertext));
    }

    [Fact]
    public void Sealing_the_same_text_twice_gives_different_ciphertexts()
    {
        using var sealer = SealProvider.Factory.CreateSealer(TestKeys.PublicKey1);

        Assert.NotEqual(sealer.Seal(Plaintext), sealer.Seal(Plaintext));
    }

    [Theory]
    [ClassData(typeof(SignerData))]
    public void Sign_gives_the_text_followed_by_a_signature_of_the_key_size(byte[] plaintext, string key, string passphrase, int expectedLength)
    {
        using var signer = SealProvider.Factory.CreateSigner(key, Passphrase(passphrase));

        var signed = signer.Sign(plaintext);

        Assert.Equal(expectedLength, signed?.Length);
        Assert.Equal(plaintext, signed.GetDataFromSignature(TestKeys.PublicKeyOf(key)));
    }

    [Fact]
    public void A_signature_is_accepted_by_the_public_key_and_refused_by_another()
    {
        using var signer = SealProvider.Factory.CreateSigner(TestKeys.PrivateKey3);
        using var verifier = SealProvider.Factory.CreateVerifier(TestKeys.PublicKey3);
        using var otherVerifier = SealProvider.Factory.CreateVerifier(TestKeys.PublicKey1);

        var signed = signer.Sign(Plaintext)!;

        Assert.True(verifier.Verify(signed));
        Assert.False(otherVerifier.Verify(signed));
    }

    [Fact]
    public void A_changed_text_no_longer_matches_its_signature()
    {
        using var signer = SealProvider.Factory.CreateSigner(TestKeys.PrivateKey3);
        using var verifier = SealProvider.Factory.CreateVerifier(TestKeys.PublicKey3);
        var signed = signer.Sign(Plaintext)!;

        signed[0] ^= 0xFF;

        Assert.False(verifier.Verify(signed));
    }

    #endregion

    #region Keys and passphrases

    [Fact]
    public void An_encrypted_key_with_a_wrong_passphrase_cannot_sign_or_unseal()
    {
        using var signer = SealProvider.Factory.CreateSigner(TestKeys.PrivateKey1, Passphrase("not the passphrase"));
        using var sealer = SealProvider.Factory.CreateSealer(TestKeys.PublicKey1);
        using var unsealer = SealProvider.Factory.CreateUnsealer(TestKeys.PrivateKey1, TestKeys.PublicKey1, Passphrase("not the passphrase"));

        Assert.Null(signer.Sign(Plaintext));
        Assert.Null(unsealer.Unseal(sealer.Seal(Plaintext)!));
    }

    [Fact]
    public void An_encrypted_key_without_a_passphrase_cannot_sign()
    {
        using var signer = SealProvider.Factory.CreateSigner(TestKeys.PrivateKey1);

        Assert.Null(signer.Sign(Plaintext));
    }

    [Fact]
    public void Text_that_is_not_a_key_cannot_sign_or_verify()
    {
        using var signer = SealProvider.Factory.CreateSigner("not a key");
        using var verifier = SealProvider.Factory.CreateVerifier("not a key");

        Assert.Null(signer.Sign(Plaintext));
        Assert.False(verifier.Verify(new byte[300]));
    }

    [Fact]
    public void GetPKeySize_gives_the_size_of_a_2048_bit_key_in_bytes()
    {
        Assert.Equal(256, SealProvider.GetPKeySize(TestKeys.PublicKey1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a key")]
    public void GetPKeySize_gives_minus_one_for_text_that_is_not_a_public_key(string key)
    {
        Assert.Equal(-1, SealProvider.GetPKeySize(key));
    }

    #endregion

    #region Damaged input

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    [InlineData(283)]
    public void Unseal_refuses_input_that_is_too_short_to_be_a_ciphertext(int length)
    {
        using var unsealer = SealProvider.Factory.CreateUnsealer(TestKeys.PrivateKey3, TestKeys.PublicKey3);

        Assert.Null(unsealer.Unseal(new byte[length]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(256)]
    public void Verify_refuses_input_that_is_too_short_to_carry_a_signature(int length)
    {
        using var verifier = SealProvider.Factory.CreateVerifier(TestKeys.PublicKey3);

        Assert.False(verifier.Verify(new byte[length]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64!")]
    [InlineData("AA==")]
    [InlineData("//8=")]
    [InlineData("AAEC")]
    public void UnsealOnion_refuses_text_that_is_not_an_onion(string onion)
    {
        using var unsealer = SealProvider.Factory.CreateUnsealer(TestKeys.PrivateKey3, TestKeys.PublicKey3);
        string? next = null;
        byte[]? content = null;

        Assert.False(unsealer.UnsealOnion(onion, ref next, ref content));
        Assert.Null(next);
        Assert.Null(content);
    }

    [Fact]
    public void UnsealOnion_refuses_an_onion_whose_length_prefix_does_not_match_its_size()
    {
        using var unsealer = SealProvider.Factory.CreateUnsealer(TestKeys.PrivateKey1, TestKeys.PublicKey1, Passphrase(TestKeys.Passphrase));
        var stored = Convert.FromBase64String((string)new OnionUnsealerData().First()[0]!);
        string? next = null;
        byte[]? content = null;

        var cutInHalf = Convert.ToBase64String(stored[..(stored.Length / 2)]);
        var oneByteMore = Convert.ToBase64String([.. stored, 0x00]);

        Assert.False(unsealer.UnsealOnion(cutInHalf, ref next, ref content));
        Assert.False(unsealer.UnsealOnion(oneByteMore, ref next, ref content));
    }

    #endregion
}
