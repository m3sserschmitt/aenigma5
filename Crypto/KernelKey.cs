using System.Runtime.InteropServices;

namespace Enigma5.Crypto;

public static class KernelKey
{
    public static int Create(string keyName, byte[] keyMaterial, string description, KernelKeyring ringId)
    {
        var nativeBuffer = KeyUtil.CopyKeyToNativeBuffer(keyMaterial);

        if (nativeBuffer == IntPtr.Zero)
        {
            return -1;
        }

        var keyId = Native.CreateKey([.. keyName], nativeBuffer, (uint)keyMaterial.Length, [.. description], ringId);

        KeyUtil.FreeKeyNativeBuffer(nativeBuffer, keyMaterial);

        return keyId;
    }

    public static int SearchKey(string keyName, string description, KernelKeyring ringId)
    => Native.SearchKey([.. keyName], [.. description], ringId);

    public static byte[]? ReadKey(int keyId)
    {
        IntPtr nativeBuffer = Marshal.AllocHGlobal(Constants.KernelKeyMaxSize);
        int keySize = Native.ReadKey(keyId, nativeBuffer);

        if (keySize < 0 || keySize > Constants.KernelKeyMaxSize)
        {
            KeyUtil.FreeKeyNativeBuffer(nativeBuffer, Constants.KernelKeyMaxSize);
            return null;
        }

        var key = KeyUtil.CopyKeyFromNativeBuffer(nativeBuffer, keySize);
        KeyUtil.FreeKeyNativeBuffer(nativeBuffer, Constants.KernelKeyMaxSize);

        return key;
    }

    public static int RemoveKey(int keyId)
    => Native.RemoveKey(keyId);
}
