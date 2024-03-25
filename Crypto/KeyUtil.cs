using System.Runtime.InteropServices;

namespace Enigma5.Crypto;

public static class KeyUtil
{
    public static void FreeKeyNativeBuffer(IntPtr nativeBuffer, int bytesCount)
    {
        Marshal.Copy(new byte[bytesCount], 0, nativeBuffer, bytesCount);
        Marshal.FreeHGlobal(nativeBuffer);
    }

    public static void FreeKeyNativeBuffer(IntPtr nativeBuffer, byte[] keyMaterial)
    {
        FreeKeyNativeBuffer(nativeBuffer, keyMaterial.Length);
        Array.Clear(keyMaterial);
    }

    public static byte[] CopyKeyFromNativeBuffer(nint source, int bytesCount)
    {
        var managedBytes = new byte[bytesCount];
        Marshal.Copy(source, managedBytes, 0, bytesCount);

        return managedBytes;
    }

    private static readonly byte[] nullByte = [ 0 ];

    public static nint CopyKeyToNativeBuffer(byte[] source)
    {
        try
        {
            var nativeBuffer = Marshal.AllocHGlobal(source.Length + 1);
            
            Marshal.Copy(source, 0, nativeBuffer, source.Length);
            Marshal.Copy(nullByte, 0, nativeBuffer + source.Length, nullByte.Length);
            
            return nativeBuffer;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }
}
