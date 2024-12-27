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

using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Enigma5.Crypto;

internal static unsafe partial class Native
{
    private static readonly List<string> Libs = ["libaenigma", "libaenigma-kernelkeys"];

    static Native()
    {
        NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, ImportResolver);
    }

    private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        IntPtr libHandle = IntPtr.Zero;
        if (Libs.Contains(libraryName))
        {
            NativeLibrary.TryLoad(libraryName, assembly, DllImportSearchPath.AssemblyDirectory, out libHandle);
        }
        return libHandle;
    }

    [LibraryImport("libaenigma")]
    internal static partial int GetPKeySize([MarshalAs(UnmanagedType.LPStr)] string publicKey);

    [LibraryImport("libaenigma-kernelkeys")]
    internal static partial uint GetKernelKeyMaxSize();

    [LibraryImport("libaenigma")]
    internal static partial IntPtr CreateAsymmetricEncryptionContext([MarshalAs(UnmanagedType.LPStr)] string publicKey);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr CreateAsymmetricDecryptionContext([MarshalAs(UnmanagedType.LPStr)] string privateKey, [MarshalAs(UnmanagedType.LPStr)] string passphrase);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr CreateSignatureContext([MarshalAs(UnmanagedType.LPStr)] string privateKey, [MarshalAs(UnmanagedType.LPStr)] string passphrase);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr CreateVerificationContext([MarshalAs(UnmanagedType.LPStr)] string publicKey);

    [LibraryImport("libaenigma")]
    internal static partial void FreeContext(IntPtr ctx);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr EncryptData(IntPtr ctx, [In] byte[] plaintext, uint plaintextLen, out int ciphertextLen);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr DecryptData(IntPtr ctx, [In] byte[] ciphertext, uint ciphertextLen, out int plaintextLen);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr SignData(IntPtr ctx, [In] byte[] plaintext, uint plaintextLen, out int signatureLen);

    [LibraryImport("libaenigma")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VerifySignature(IntPtr ctx, [In] byte[] ciphertext, uint ciphertextLen);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr UnsealOnion(IntPtr ctx, [In] byte[] onion, out int outLen);

    [LibraryImport("libaenigma", StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
    internal static partial IntPtr SealOnion([In] byte[] plaintext, uint plaintextLen, [In] string[] keys, [In] string[] addresses, uint count, out int outLen);

    [LibraryImport("libaenigma-kernelkeys")]
    internal static partial int CreateKey([MarshalAs(UnmanagedType.LPStr)] string keyName, [MarshalAs(UnmanagedType.LPStr)] string keyMaterial, uint keyMaterialSize, [MarshalAs(UnmanagedType.LPStr)] string description, KernelKeyring ringId);

    [LibraryImport("libaenigma-kernelkeys")]
    internal static partial int SearchKey([MarshalAs(UnmanagedType.LPStr)] string keyName, [MarshalAs(UnmanagedType.LPStr)] string description, KernelKeyring ringId);

    [LibraryImport("libaenigma-kernelkeys")]
    internal static partial int ReadKey(int keyId, [Out] byte[] keyMaterial);

    [LibraryImport("libaenigma-kernelkeys")]
    internal static partial int RemoveKey(int keyId);
}
