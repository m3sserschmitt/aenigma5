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

using Enigma5.Crypto.Extensions;

namespace Enigma5.Crypto;

internal sealed class CryptoContext : IDisposable
{
    private bool disposed = false;

    private IntPtr handle;

    private CryptoContext(IntPtr handle)
    {
        this.handle = handle;
    }

    ~CryptoContext()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public bool IsNull => handle == IntPtr.Zero;

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
            }

            Native.FreeContext(handle);
            handle = IntPtr.Zero;

            disposed = true;
        }
    }

    public static implicit operator IntPtr(CryptoContext envelopeContext)
    {
        return envelopeContext.handle;
    }

    internal static class Factory
    {
        public static CryptoContext CreateAsymmetricEncryptionContext(string publicKey)
        => new(publicKey.IsValidPublicKey() ? Native.CreateAsymmetricEncryptionContext(publicKey) : IntPtr.Zero);

        public static CryptoContext CreateAsymmetricDecryptionContext(string privateKey, string passphrase)
        => new(privateKey.IsValidPrivateKey() ? Native.CreateAsymmetricDecryptionContext(privateKey, passphrase) : IntPtr.Zero);

        public static CryptoContext CreateSignatureContext(string privateKey, string passphrase)
        => new(privateKey.IsValidPrivateKey() ? Native.CreateSignatureContext(privateKey, passphrase) : IntPtr.Zero);

        public static CryptoContext CreateSignatureVerificationContext(string publicKey)
        => new(publicKey.IsValidPublicKey() ? Native.CreateVerificationContext(publicKey) : IntPtr.Zero);
    }
}
