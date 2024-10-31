/*
    Aenigma - Federal messaging system
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

using Enigma5.Crypto.Tests.TestData;
using FluentAssertions;
using Xunit;

namespace Enigma5.Crypto.Tests;

public class SealProviderTests
{
    [Theory]
    [ClassData(typeof(SealerData))]
    public void ShouldSeal(byte[] plaintext, string key, int expectedCiphertextLength)
    {
        // Arrange
        using var seal = SealProvider.Factory.CreateSealer(key);

        // Act
        var ciphertext = seal.Seal(plaintext);

        // Assert
        ciphertext.Should().NotBeNull();
        ciphertext!.Length.Should().Be(expectedCiphertextLength);
    }

    [Theory]
    [ClassData(typeof(UnsealerData))]
    public void ShouldUnseal(byte[] ciphertext, string key, string passphrase, byte[]? expectedPlaintext)
    {
        // Arrange
        using var unseal = SealProvider.Factory.CreateUnsealer(key, passphrase);

        // Act
        var plaintext = unseal.Unseal(ciphertext);

        // Assert
        plaintext.Should().Equal(expectedPlaintext);
    }

    [Theory]
    [ClassData(typeof(SignerData))]
    public void ShouldSign(byte[] plaintext, string key, string passphrase, int expectedSignatureLength)
    {
        // Arrange
        using var signature = SealProvider.Factory.CreateSigner(key, passphrase);

        // Act
        var ciphertext = signature.Sign(plaintext);

        // Assert
        ciphertext.Should().NotBeNull();
        ciphertext!.Length.Should().Be(expectedSignatureLength);
        ciphertext.Take(plaintext.Length).Should().Equal(plaintext);
    }

    [Theory]
    [ClassData(typeof(VerifierData))]
    public void ShouldVerifySignature(byte[] signedData, string key, bool expectedResult)
    {
        // Arrange
        using var signatureVerier = SealProvider.Factory.CreateVerifier(key);

        // Act
        var result = signatureVerier.Verify(signedData);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [ClassData(typeof(OnionSealerData))]
    public void ShouldSealOnion(byte[] plaintext, List<string> keys, List<string> addresses, int expectedOnionLength)
    {
        // Arrange

        // Act
        var onion = SealProvider.SealOnion(plaintext, keys, addresses);
        
        // Assert
        onion.Should().NotBeNull();
        onion!.Length.Should().Be(expectedOnionLength);
    }

    [Theory]
    [ClassData(typeof(OnionUnsealerData))]
    public void ShouldUnsealOnion(string onion, string key, string passphrase, string? expectedNext, byte[]? expectedPlaintext)
    {
        // Arrange
        using var unsealer = SealProvider.Factory.CreateUnsealer(key, passphrase);

        // Act
        string? next = null;
        byte[]? content = null;
        unsealer.UnsealOnion(onion, ref next, ref content);

        // Assert
        content.Should().Equal(expectedPlaintext);
        next.Should().Be(expectedNext);
    }
}
