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
using Enigma5.Crypto.Extensions;

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
        if(_ctx.IsNull || input.Length == 0)
        {
            return null;
        }

        var outputPtr = executor(_ctx, input, (uint)input.Length, out int outputSize);

        if (outputPtr == IntPtr.Zero || outputSize < 0)
        {
            return null;
        }

        return KeyUtil.CopyKeyFromNativeBuffer(outputPtr, outputSize);
    }

    public byte[]? Seal(byte[] plaintext) => Execute(plaintext, Native.EncryptData);

    public byte[]? Unseal(byte[] ciphertext) => Execute(ciphertext, Native.DecryptData);

    public bool UnsealOnion(string onion, ref string? next, ref byte[]? content)
    {
        if(_ctx.IsNull || string.IsNullOrWhiteSpace(onion))
        {
            return false;
        }

        try
        {
            var decodedOnion = Convert.FromBase64String(onion);

            if(decodedOnion is null)
            {
                return false;
            }

            var data = Native.UnsealOnion(_ctx, decodedOnion, out var outLen);

            if (data == IntPtr.Zero || outLen < Constants.AddressSize)
            {
                return false;
            }

            var nextBytes = KeyUtil.CopyKeyFromNativeBuffer(data, Constants.AddressSize);
            next = null;
            if(nextBytes is not null)
            {
                next = HashProvider.ToHex(nextBytes);
            }
            content = KeyUtil.CopyKeyFromNativeBuffer(data + Constants.AddressSize, outLen - Constants.AddressSize);

            return next is not null && content is not null;
        }
        catch(Exception)
        {
            return false;
        }
    }

    public static int GetPKeySize(string publicKey) => publicKey.IsValidPublicKey() ? Native.GetPKeySize(publicKey) : -1;

    public static string? SealOnion(
        byte[] plaintext,
        List<string> keys,
        List<string> addresses)
    {
        if (keys.Count != addresses.Count || keys.Any(item => !item.IsValidPublicKey()) || addresses.Any(item => !item.IsValidAddress()) || plaintext.Length == 0)
        {
            return null;
        }

        var data = Native.SealOnion(plaintext, (uint)plaintext.Length, [.. keys], [.. addresses], (uint)keys.Count, out var outLen);

        if (data == IntPtr.Zero || outLen < 0)
        {
            return null;
        }

        var managedBuffer = KeyUtil.CopyKeyFromNativeBuffer(data, outLen);
        KeyUtil.FreeKeyNativeBuffer(data, outLen);
        
        return managedBuffer is not null ? Convert.ToBase64String(managedBuffer) : null;
    }

    public byte[]? Sign(byte[] plaintext) => !_ctx.IsNull ? Execute(plaintext, Native.SignData) : null;

    public bool Verify(byte[] ciphertext) => !_ctx.IsNull && ciphertext.Length > 0 && Native.VerifySignature(_ctx, ciphertext, (uint)ciphertext.Length);

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

        public static IEnvelopeSealer CreateSealer(string key)
        => new SealProvider(CryptoContext.Factory.CreateAsymmetricEncryptionContext(key));
    }
}
