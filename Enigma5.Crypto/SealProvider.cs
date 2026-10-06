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
using Enigma5.Crypto.Contracts;

namespace Enigma5.Crypto;

public sealed class SealProvider :
    IDisposable,
    IEnvelopeSealer,
    IEnvelopeUnsealer,
    IEnvelopeSigner,
    IEnvelopeVerifier
{
    private bool _disposed;

    private readonly CryptoContext _ctx;

    // Sizes derived from the public key matching this context, or -1 when unknown;
    // libaenigma trusts input lengths, so inputs are checked against these before any native call.
    private readonly int _envelopeOverhead;

    private readonly int _signatureSize;

    private SealProvider(CryptoContext ctx, string? publicKey = null)
    {
        _ctx = ctx;
        var validPublicKey = publicKey.IsValidPublicKey();
        _envelopeOverhead = validPublicKey ? Native.GetEnvelopeSize(0, publicKey!) : -1;
        _signatureSize = validPublicKey ? Native.GetSignedDataSize(0, publicKey!) : -1;
    }

    ~SealProvider()
    {
        Dispose(false);
    }

    private delegate IntPtr NativeExecutor(IntPtr ctx, byte[] inputData, uint inputSize, out int outputSize);

    private byte[]? Execute(byte[] input, NativeExecutor executor)
    {
        if (_ctx.IsNull || input.Length == 0)
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

    public byte[]? Seal(byte[] plaintext) => Execute(plaintext, Native.Run);

    public byte[]? Unseal(byte[] ciphertext)
    => _envelopeOverhead >= 0 && ciphertext.Length > _envelopeOverhead ? Execute(ciphertext, Native.Run) : null;

    private bool IsWellFormedOnion(byte[] onion)
    {
        if (_envelopeOverhead < 0 || onion.Length < Constants.OnionLengthBytes)
        {
            return false;
        }

        long envelopeSize = Native.DecodeOnionSize(onion);
        return envelopeSize == onion.Length - Constants.OnionLengthBytes
            && envelopeSize - _envelopeOverhead >= Constants.AddressSize;
    }

    public bool UnsealOnion(string onion, ref string? next, ref byte[]? content)
    {
        if (_ctx.IsNull || string.IsNullOrWhiteSpace(onion))
        {
            return false;
        }

        try
        {
            var decodedOnion = Convert.FromBase64String(onion);

            if (decodedOnion is null || !IsWellFormedOnion(decodedOnion))
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
            if (nextBytes is not null)
            {
                next = HashProvider.ToHex(nextBytes);
            }
            content = KeyUtil.CopyKeyFromNativeBuffer(data + Constants.AddressSize, outLen - Constants.AddressSize);

            return next is not null && content is not null;
        }
        catch (Exception)
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

    public static bool SetMasterPassphraseName(string name) => Native.SetMasterPassphraseName(name);

    public static int SearchPersistentMasterPassphrase() => Native.SearchPersistentMasterPassphrase();

    public static int SearchMasterPassphrase() => Native.SearchMasterPassphrase();

    public static int CreateMasterPassphrase(byte[] passphrase)
    => WithTerminatedCopy(passphrase, Native.CreateMasterPassphrase);

    public static int CreatePersistentMasterPassphrase(byte[] passphrase)
    => WithTerminatedCopy(passphrase, Native.CreatePersistentMasterPassphrase);

    // libaenigma reads the passphrase as a zero-terminated string of at most KernelKeyMaxSize bytes.
    private static int WithTerminatedCopy(byte[] passphrase, Func<byte[], int> create)
    {
        if (passphrase.Length == 0 || passphrase.Length > Constants.KernelKeyMaxSize || Array.IndexOf(passphrase, (byte)0) >= 0)
        {
            return -1;
        }

        var terminated = new byte[passphrase.Length + 1];
        try
        {
            passphrase.CopyTo(terminated, 0);
            return create(terminated);
        }
        finally
        {
            Array.Clear(terminated);
        }
    }

    public static bool RemoveMasterPassphrase() => Native.RemoveMasterPassphrase();

    public byte[]? Sign(byte[] plaintext) => !_ctx.IsNull ? Execute(plaintext, Native.Run) : null;

    public bool Verify(byte[] ciphertext)
    => !_ctx.IsNull
    && _signatureSize >= 0
    && ciphertext.Length > _signatureSize
    && Native.RunVerification(_ctx, ciphertext, (uint)ciphertext.Length);

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
            _ctx.Dispose();
            _disposed = true;
        }
    }

    public static class Factory
    {
        public static IEnvelopeSigner CreateSigner(string key, byte[]? passphrase)
        => new SealProvider(CryptoContext.Factory.CreateSignatureContext(key, passphrase));

        public static IEnvelopeSigner CreateSignerFromFile(string path, byte[]? passphrase)
        => new SealProvider(CryptoContext.Factory.CreateSignatureContextFromFile(path, passphrase));

        public static IEnvelopeSigner CreateSigner(string key)
        => CreateSigner(key, null);

        public static IEnvelopeSigner CreateSignerFromFile(string path)
        => CreateSignerFromFile(path, null);

        public static IEnvelopeVerifier CreateVerifier(string key)
        => new SealProvider(CryptoContext.Factory.CreateSignatureVerificationContext(key), key);

        public static IEnvelopeUnsealer CreateUnsealer(string key, string publicKey, byte[]? passphrase)
        => new SealProvider(CryptoContext.Factory.CreateAsymmetricDecryptionContext(key, passphrase), publicKey);

        public static IEnvelopeUnsealer CreateUnsealerFromFile(string path, string publicKey, byte[]? passphrase)
        => new SealProvider(CryptoContext.Factory.CreateAsymmetricDecryptionContextFromFile(path, passphrase), publicKey);

        public static IEnvelopeUnsealer CreateUnsealer(string key, string publicKey)
        => CreateUnsealer(key, publicKey, null);

        public static IEnvelopeUnsealer CreateUnsealerFromFile(string path, string publicKey)
        => CreateUnsealerFromFile(path, publicKey, null);

        public static IEnvelopeSealer CreateSealer(string key)
        => new SealProvider(CryptoContext.Factory.CreateAsymmetricEncryptionContext(key));
    }
}
