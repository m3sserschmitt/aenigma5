using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

internal static unsafe partial class Native
{
    [LibraryImport("cryptography", StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
    internal static partial IntPtr SealOnion(
        [In] byte[] plaintext,
        uint plaintextLen,
        [In] string[] keys,
        [In] string[] addresses,
        uint count,
        out int outLen);
}
