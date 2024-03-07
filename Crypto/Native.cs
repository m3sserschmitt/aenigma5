using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Enigma5.Crypto;

internal static unsafe partial class Native
{
    [LibraryImport("cryptography")]
    internal static partial uint GetDefaultAddressSize();

    [LibraryImport("cryptography")]
    internal static partial uint GetDefaultPKeySize();
    
    [LibraryImport("cryptography")]
    internal static partial IntPtr CreateAsymmetricEncryptionContext(
        [MarshalAs(UnmanagedType.LPStr)] string key);

    [LibraryImport("cryptography")]
    internal static partial IntPtr CreateAsymmetricDecryptionContext(
        [MarshalAs(UnmanagedType.LPStr)] string key,
        [MarshalAs(UnmanagedType.LPStr)] string passphrase);

    [LibraryImport("cryptography")]
    internal static partial IntPtr CreateAsymmetricEncryptionContextFromFile(
        [MarshalAs(UnmanagedType.LPStr)] string path);

    [LibraryImport("cryptography")]
    internal static partial IntPtr CreateAsymmetricDecryptionContextFromFile(
        [MarshalAs(UnmanagedType.LPStr)] string path,
        [MarshalAs(UnmanagedType.LPStr)] string passphrase);

    [LibraryImport("cryptography")]
    internal static partial IntPtr CreateSignatureContext(
        [MarshalAs(UnmanagedType.LPStr)] string key,
        [MarshalAs(UnmanagedType.LPStr)] string passphrase);

    [LibraryImport("cryptography")]
    internal static partial IntPtr CreateVerificationContext(
        [MarshalAs(UnmanagedType.LPStr)] string key);

    [LibraryImport("cryptography")]
    internal static partial IntPtr CreateSignatureContextFromFile(
        [MarshalAs(UnmanagedType.LPStr)] string path,
        [MarshalAs(UnmanagedType.LPStr)] string passphrase);

    [LibraryImport("cryptography")]
    internal static partial IntPtr CreateVerificationContextFromFile(
        [MarshalAs(UnmanagedType.LPStr)] string path);

    [LibraryImport("cryptography")]
    internal static partial void FreeContext(IntPtr ctx);

    [LibraryImport("cryptography")]
    internal static partial IntPtr EncryptData(
        IntPtr ctx,
        [In] byte[] plaintext,
        uint plaintextLen,
        out int ciphertextLen);

    [LibraryImport("cryptography")]
    internal static partial IntPtr DecryptData(
        IntPtr ctx,
        [In] byte[]
        ciphertext,
        uint ciphertextLen,
        out int plaintextLen);

    [LibraryImport("cryptography")]
    internal static partial IntPtr SignData(
        IntPtr ctx,
        [In] byte[]
        plaintext,
        uint plaintextLen,
        out int signatureLen);

    [LibraryImport("cryptography")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool VerifySignature(IntPtr ctx,
    [In] byte[] ciphertext,
    uint ciphertextLen);

    [LibraryImport("cryptography")]
    internal static partial uint GetEnvelopeSize(uint plaintextLen);

    [LibraryImport("cryptography")]
    internal static partial uint GetOpenEnvelopeSize(uint envelopeSize);

    [LibraryImport("cryptography")]
    internal static partial uint GetSignedDataSize(uint dataLen);

    [LibraryImport("cryptography")]
    internal static partial IntPtr UnsealOnion(IntPtr ctx, [In] byte[] onion, out int outLen);

    [LibraryImport("cryptography", StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
    internal static partial IntPtr SealOnion(
        [In] byte[] plaintext,
        uint plaintextLen,
        [In] string[] keys,
        [In] string[] addresses,
        uint count,
        out int outLen);
}
