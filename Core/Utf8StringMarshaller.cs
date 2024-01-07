using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;
using System.Text;

namespace Enigma5.Core;

[CustomMarshaller(typeof(string), MarshalMode.Default, typeof(Utf8StringMarshaller))]
[CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanagedIn))]
internal static unsafe class Utf8StringMarshaller
{
    public static uint* ConvertToUnmanaged(string? managed)
    {
        if (managed is null)
            return null;

        int exactByteCount = checked(Encoding.UTF8.GetByteCount(managed) + 1);
        uint* mem = (uint*)NativeMemory.Alloc((nuint)exactByteCount);
        Span<byte> buffer = new(mem, exactByteCount);

        int byteCount = Encoding.UTF8.GetBytes(managed, buffer);
        buffer[byteCount..].Fill(0);
        return mem;
    }

    public static string? ConvertToManaged(uint* unmanaged)
    {
        if (unmanaged == null)
            return null;

        var toSearch = new Span<uint>(unmanaged, int.MaxValue);
        int len = toSearch.IndexOf((uint)0);

        return Encoding.UTF8.GetString((byte*)unmanaged, len * sizeof(uint));
    }

    public static void Free(uint* unmanaged)
        => NativeMemory.Free(unmanaged);

    public ref struct ManagedToUnmanagedIn
    {
        public static int BufferSize => 0x100;

        private uint* _unmanagedValue;
        private bool _allocated;

        public void FromManaged(string? managed, Span<byte> buffer)
        {
            _allocated = false;

            if (managed is null)
            {
                _unmanagedValue = null;
                return;
            }

            int exactByteCount = checked(Encoding.UTF8.GetByteCount(managed) + 4); // + 4 for null terminator
            if (exactByteCount > buffer.Length)
            {
                buffer = new Span<byte>((byte*)NativeMemory.Alloc((nuint)exactByteCount), exactByteCount);
                _allocated = true;
            }

            _unmanagedValue = (uint*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));

            int byteCount = Encoding.UTF8.GetBytes(managed, buffer);
            buffer[byteCount..].Fill(0); // null-terminate
        }

        public readonly uint* ToUnmanaged() => _unmanagedValue;

        public readonly void Free()
        {
            if (_allocated)
                NativeMemory.Free(_unmanagedValue);
        }
    }
}
