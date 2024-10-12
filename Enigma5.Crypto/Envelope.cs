/*
    Aenigma - Onion Routing based messaging application
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

using Enigma5.Crypto.Contracts;

namespace Enigma5.Crypto;

public sealed class Envelope :
    IDisposable,
    IEnvelopeUnseal,
    IEnvelopeSeal,
    IEnvelopeSign,
    IEnvelopeVerify
{
    private readonly SealProvider _sealProvider;

    private Envelope(SealProvider sealProvider)
    {
        _sealProvider = sealProvider;
    }

    ~Envelope()
    {
        _sealProvider.Dispose();
    }

    public byte[]? Seal(byte[] plaintext) => _sealProvider.Seal(plaintext);

    public byte[]? Unseal(byte[] ciphertext) => _sealProvider.Unseal(ciphertext);

    public IntPtr UnsealOnion(byte[] onion, out int outLen)
    => _sealProvider.UnsealOnion(onion, out outLen);

    public static IntPtr SealOnion(
        byte[] plaintext,
        string[] keys,
        string[] addresses,
        out int outLen)
    => SealProvider.SealOnion(plaintext, keys, addresses, out outLen);

    public byte[]? Sign(byte[] plaintext) => _sealProvider.Sign(plaintext);

    public bool Verify(byte[] ciphertext) => _sealProvider.Verify(ciphertext);

    public static int GetEnvelopeSize(int plaintextLen)
    => (int)Native.GetEnvelopeSize((uint)plaintextLen);

    public void Dispose()
    {
        _sealProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    public static class Factory
    {
        public static IEnvelopeSeal CreateSealFromFile(string path)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateAsymmetricEncryptionContextFromFile(path)));

        public static IEnvelopeUnseal CreateUnsealFromFile(string path, string passphrase)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateAsymmetricDecryptionContextFromFile(path, passphrase)));

        public static IEnvelopeSeal CreateSeal(string key)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateAsymmetricEncryptionContext(key)));

        public static IEnvelopeUnseal CreateUnseal(byte[] key, string passphrase)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateAsymmetricDecryptionContext(key, passphrase)));

        public static IEnvelopeSign CreateSignature(byte[] key, string passphrase)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateSignatureContext(key, passphrase)));

        public static IEnvelopeSign CreateSignatureFromFile(string path, string passphrase)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateSignatureContextFromFile(path, passphrase)));

        public static IEnvelopeVerify CreateSignatureVerification(string key)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateSignatureVerificationContext(key)));

        public static IEnvelopeVerify CreateSignatureVerificationFromFile(string path)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateSignatureVerificationContextFromFile(path)));
    }
}
