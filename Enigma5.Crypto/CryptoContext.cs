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

namespace Enigma5.Crypto;

internal sealed class CryptoContext : IDisposable
{
    private bool disposed = false;

    private IntPtr handle;

    private CryptoContext(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            throw new Exception("Encryption context is null.");
        }

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
        public static CryptoContext CreateAsymmetricEncryptionContext(string key)
        {
            return new CryptoContext(Native.CreateAsymmetricEncryptionContext(key));
        }

        public static CryptoContext CreateAsymmetricDecryptionContext(byte[] key, string passphrase)
        {
            var nativeBuffer = KeyUtil.CopyKeyToNativeBuffer(key);
            var ctx = new CryptoContext(Native.CreateAsymmetricDecryptionContext(nativeBuffer, passphrase));
            KeyUtil.FreeKeyNativeBuffer(nativeBuffer, key);

            return ctx;
        }

        public static CryptoContext CreateAsymmetricEncryptionContextFromFile(string path)
        {
            return new CryptoContext(Native.CreateAsymmetricEncryptionContextFromFile(path));
        }

        public static CryptoContext CreateAsymmetricDecryptionContextFromFile(string path, string passphrase)
        {
            return new CryptoContext(Native.CreateAsymmetricDecryptionContextFromFile(path, passphrase));
        }

        public static CryptoContext CreateSignatureContext(byte[] key, string passphrase)
        {
            var nativeBuffer = KeyUtil.CopyKeyToNativeBuffer(key);
            var ctx = new CryptoContext(Native.CreateSignatureContext(nativeBuffer, passphrase));
            KeyUtil.FreeKeyNativeBuffer(nativeBuffer, key);

            return ctx;
        }

        public static CryptoContext CreateSignatureContextFromFile(string path, string passphrase)
        {
            return new CryptoContext(Native.CreateSignatureContextFromFile(path, passphrase));
        }

        public static CryptoContext CreateSignatureVerificationContext(string key)
        {
            return new CryptoContext(Native.CreateVerificationContext(key));
        }

        public static CryptoContext CreateSignatureVerificationContextFromFile(string path)
        {
            return new CryptoContext(Native.CreateVerificationContextFromFile(path));
        }
    }
}
