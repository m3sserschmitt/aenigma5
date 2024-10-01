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
    internal static partial uint GetDefaultAddressSize();

    [LibraryImport("libaenigma")]
    internal static partial uint GetDefaultPKeySize();

    [LibraryImport("libaenigma-kernelkeys")]
    internal static partial uint GetKernelKeyMaxSize();

    [LibraryImport("libaenigma")]
    internal static partial IntPtr CreateAsymmetricEncryptionContext(
        [MarshalAs(UnmanagedType.LPStr)] string key);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr CreateAsymmetricDecryptionContext(
        IntPtr key,
        [MarshalAs(UnmanagedType.LPStr)] string passphrase);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr CreateAsymmetricEncryptionContextFromFile(
        [MarshalAs(UnmanagedType.LPStr)] string path);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr CreateAsymmetricDecryptionContextFromFile(
        [MarshalAs(UnmanagedType.LPStr)] string path,
        [MarshalAs(UnmanagedType.LPStr)] string passphrase);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr CreateSignatureContext(
        IntPtr key,
        [MarshalAs(UnmanagedType.LPStr)] string passphrase);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr CreateVerificationContext(
        [MarshalAs(UnmanagedType.LPStr)] string key);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr CreateSignatureContextFromFile(
        [MarshalAs(UnmanagedType.LPStr)] string path,
        [MarshalAs(UnmanagedType.LPStr)] string passphrase);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr CreateVerificationContextFromFile(
        [MarshalAs(UnmanagedType.LPStr)] string path);

    [LibraryImport("libaenigma")]
    internal static partial void FreeContext(IntPtr ctx);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr EncryptData(
        IntPtr ctx,
        [In] byte[] plaintext,
        uint plaintextLen,
        out int ciphertextLen);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr DecryptData(
        IntPtr ctx,
        [In] byte[]
        ciphertext,
        uint ciphertextLen,
        out int plaintextLen);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr SignData(
        IntPtr ctx,
        [In] byte[]
        plaintext,
        uint plaintextLen,
        out int signatureLen);

    [LibraryImport("libaenigma")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VerifySignature(IntPtr ctx,
    [In] byte[] ciphertext,
    uint ciphertextLen);

    [LibraryImport("libaenigma")]
    internal static partial uint GetEnvelopeSize(uint plaintextLen);

    [LibraryImport("libaenigma")]
    internal static partial uint GetOpenEnvelopeSize(uint envelopeSize);

    [LibraryImport("libaenigma")]
    internal static partial uint GetSignedDataSize(uint dataLen);

    [LibraryImport("libaenigma")]
    internal static partial IntPtr UnsealOnion(IntPtr ctx, [In] byte[] onion, out int outLen);

    [LibraryImport("libaenigma", StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
    internal static partial IntPtr SealOnion(
        [In] byte[] plaintext,
        uint plaintextLen,
        [In] string[] keys,
        [In] string[] addresses,
        uint count,
        out int outLen);

    [LibraryImport("libaenigma-kernelkeys")]
    internal static partial int CreateKey(
        [In] char[] keyName,
        IntPtr keyMaterial,
        uint keyMaterialSize,
        [In] char[] description,
        KernelKeyring ringId);

    [LibraryImport("libaenigma-kernelkeys")]
    internal static partial int SearchKey(
        [In] char[] keyName,
        [In] char[] description,
        KernelKeyring ringId
    );

    [LibraryImport("libaenigma-kernelkeys")]
    internal static partial int ReadKey(
        int keyId,
        IntPtr keyMaterial
    );

    [LibraryImport("libaenigma-kernelkeys")]
    internal static partial int RemoveKey(
        int keyId
    );
}
