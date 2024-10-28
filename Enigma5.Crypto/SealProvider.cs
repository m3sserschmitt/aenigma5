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

using Enigma5.Crypto.Contracts;
using System.Runtime.InteropServices;

namespace Enigma5.Crypto;

public sealed class SealProvider :
    IDisposable,
    IEnvelopeSealer,
    IEnvelopeUnsealer,
    IEnvelopeSigner,
    IEnvelopeVerifier
{
    private readonly CryptoContext _ctx;

    private SealProvider(CryptoContext ctx)
    {
        _ctx = ctx;
    }

    ~SealProvider()
    {
        _ctx.Dispose();
    }

    private delegate IntPtr NativeExecutor(IntPtr ctx, byte[] inputData, uint inputSize, out int outputSize);

    private byte[]? Execute(byte[] input, NativeExecutor executor)
    {
        IntPtr outputPtr = executor(_ctx, input, (uint)input.Length, out int outputSize);

        if (outputPtr == IntPtr.Zero || outputSize < 0)
        {
            return null;
        }

        var output = new byte[outputSize];

        Marshal.Copy(outputPtr, output, 0, outputSize);

        return output;
    }

    public byte[]? Seal(byte[] plaintext) => Execute(plaintext, Native.EncryptData);

    public byte[]? Unseal(byte[] ciphertext) => Execute(ciphertext, Native.DecryptData);

    public IntPtr UnsealOnion(byte[] ciphertext, out int outLen) => Native.UnsealOnion(_ctx, ciphertext, out outLen);

    public static int GetPKeySize(string publicKey) => (int)Native.GetPKeySize(publicKey);

    public static IntPtr SealOnion(
        byte[] plaintext,
        string[] keys,
        string[] addresses,
        out int outLen)
    {
        if (keys.Length != addresses.Length)
        {
            throw new ArgumentException("Number of keys should equal the number of addresses.");
        }

        return Native.SealOnion(plaintext, (uint)plaintext.Length, keys, addresses, (uint)keys.Length, out outLen);
    }

    public byte[]? Sign(byte[] plaintext) => Execute(plaintext, Native.SignData);

    public bool Verify(byte[] ciphertext) => Native.VerifySignature(_ctx, ciphertext, (uint)ciphertext.Length);

    public void Dispose()
    {
        _ctx.Dispose();
        GC.SuppressFinalize(this);
    }

    public static class Factory
    {
        public static IEnvelopeSigner CreateSigner(string key, string passphrase)
        => new SealProvider(CryptoContext.Factory.CreateSignatureContext(key, passphrase));

        public static IEnvelopeSigner CreateSigner(string key)
        => CreateSigner(key, string.Empty);

        public static IEnvelopeVerifier CreateVerifier(string key)
        => new SealProvider(CryptoContext.Factory.CreateSignatureVerificationContext(key));

        public static IEnvelopeUnsealer CreateUnsealer(string key, string passphrase)
        => new SealProvider(CryptoContext.Factory.CreateAsymmetricDecryptionContext(key, passphrase));

        public static IEnvelopeUnsealer CreateUnsealer(string key)
        => CreateUnsealer(key, string.Empty);
    }

}
