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

using System.Text;
using Enigma5.Crypto.DataProviders;
using Xunit;

namespace Enigma5.Crypto.Tests;

public class EnvelopeTests
{
    [Fact]
    public void Envelope_ShouldSeal()
    {
        // Arrange
        using var seal = Envelope.Factory.CreateSeal(PKey.PublicKey1);
        byte[] plaintext = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        // Act
        var ciphertext = seal.Seal(plaintext);

        // Assert
        Assert.NotNull(ciphertext);
        Assert.Equal(256 + 12 + 16 + plaintext.Length, ciphertext!.Length);
    }

    [Fact]
    public void Envelope_ShouldUnseal()
    {
        // Arrange
        var testEnvelope = TestEnvelope.Create();
        using var unseal = Envelope.Factory.CreateUnseal(Encoding.UTF8.GetBytes(PKey.PrivateKey1), PKey.Passphrase);

        // Act
        var plaintext = unseal.Unseal(testEnvelope);

        // Assert
        Assert.NotNull(plaintext);
        Assert.Equal(testEnvelope.ExpectedPlaintext, plaintext);
    }

    [Fact]
    public void Envelope_ShouldSign()
    {
        // Arrange
        using var signature = Envelope.Factory.CreateSignature(Encoding.UTF8.GetBytes(PKey.PrivateKey1), PKey.Passphrase);
        byte[] plaintext = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        // Act
        var ciphertext = signature.Sign(plaintext);

        // Assert
        Assert.NotNull(ciphertext);
        Assert.Equal(plaintext.Length + 256, ciphertext!.Length);
        Assert.Equal(plaintext, ciphertext!.Take(plaintext.Length));
    }

    [Fact]
    public void Envelope_ShouldVerifySignature()
    {
        // Arrange
        var testSignature = TestSignature.CreateValidSignature();
        using var signatureVerier = Envelope.Factory.CreateSignatureVerification(PKey.PublicKey1);

        // Act
        var valid = signatureVerier.Verify(testSignature);

        // Assert
        Assert.True(valid);
    }

    [Fact]
    public void Envelope_ShouldDetectInvalidSignature()
    {
        // Arrange
        var testSignature = TestSignature.CreateInvalidSignature();
        using var signatureVerier = Envelope.Factory.CreateSignatureVerification(PKey.PublicKey1);

        // Act
        var valid = signatureVerier.Verify(testSignature);

        // Assert
        Assert.False(valid);
    }
}
