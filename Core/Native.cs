using System.Runtime.InteropServices;

namespace Enigma5.Core;

internal unsafe partial class Native
{
    [LibraryImport("cryptography")]
    internal static partial uint GetDefaultAddressSize();

    [LibraryImport("cryptography")]
    internal static partial uint GetDefaultPKeySize();
}
