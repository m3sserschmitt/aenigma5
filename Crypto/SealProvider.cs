using Enigma5.Crypto.Contracts;
using System.Runtime.InteropServices;

namespace Enigma5.Crypto;

internal sealed class SealProvider :
    IDisposable,
    IEnvelopeSeal,
    IEnvelopeUnseal,
    IEnvelopeSign,
    IEnvelopeVerify
{
    private readonly CryptoContext _ctx;

    private SealProvider(CryptoContext ctx)
    {
        _ctx = ctx;
    }

    ~SealProvider()
    {
        _ctx.Dispose();
    }

    private delegate IntPtr NativeExecutor(IntPtr ctx, byte[] inputData, uint inputSize, out int outputSize);

    private byte[]? Execute(byte[] input, NativeExecutor executor)
    {
        IntPtr outputPtr = executor(_ctx, input, (uint)input.Length, out int outputSize);

        if (outputPtr == IntPtr.Zero || outputSize < 0)
        {
            return null;
        }

        var output = new byte[outputSize];

        Marshal.Copy(outputPtr, output, 0, outputSize);

        return output;
    }

    public byte[]? Seal(byte[] plaintext)
    => Execute(plaintext, Native.EncryptData);

    public byte[]? Unseal(byte[] ciphertext)
    => Execute(ciphertext, Native.DecryptData);

    public IntPtr UnsealOnion(byte[] ciphertext, out int outLen)
    => Native.UnsealOnion(_ctx, ciphertext, out outLen);

    public byte[]? Sign(byte[] plaintext)
    => Execute(plaintext, Native.SignData);

    public bool Verify(byte[] ciphertext)
    => Native.VerifySignature(_ctx, ciphertext, (uint)ciphertext.Length);

    public void Dispose()
    {
        _ctx.Dispose();
        GC.SuppressFinalize(this);
    }

    public static class Factory
    {
        public static SealProvider Create(CryptoContext ctx) => new(ctx);
    }

}
