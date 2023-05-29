using Enigma5.Crypto.Contracts;
using System.Runtime.InteropServices;

namespace Enigma5.Crypto;

public sealed class SealProvider : IEnvelopeSeal, IEnvelopeUnseal
{
    private EnvelopeContext ctx;

    private SealProvider(EnvelopeContext ctx)
    {
        this.ctx = ctx;
    }

    [DllImport("cryptography")]
    private static extern IntPtr RsaEncrypt(IntPtr ctx, byte[] plaintext, uint plaintextLen);

    [DllImport("cryptography")]
    private static extern IntPtr RsaDecrypt(IntPtr ctx, byte[] ciphertext, uint ciphertextLen);

    [DllImport("cryptography")]
    public static extern uint GetEnvelopeSize(uint pkeySizeBits, uint plaintextLen);

    [DllImport("cryptography")]
    public static extern uint GetOpenEnvelopeSize(uint pkeySizeBits, uint envelopeSize);

    public byte[]? Seal(byte[] plaintext)
    {
        IntPtr ciphertextPtr = RsaEncrypt(ctx, plaintext, (uint)plaintext.Length);
        uint ciphertextLen = GetEnvelopeSize((uint)2048, (uint)plaintext.Length);
        byte[] ciphertext = new byte[ciphertextLen];

        if(ciphertextPtr == IntPtr.Zero)
        {
            return null;
        }

        Marshal.Copy(ciphertextPtr, ciphertext, 0, (int) ciphertextLen);

        return ciphertext;
    }

    public byte[]? Unseal(byte[] ciphertext)
    {
        IntPtr plaintextPtr = RsaDecrypt(ctx, ciphertext, (uint)ciphertext.Length);
        uint plaintextLen = GetOpenEnvelopeSize((uint)2048, (uint)ciphertext.Length);
        byte[] plaintext = new byte[plaintextLen];

        if(plaintextPtr == IntPtr.Zero)
        {
            return null;
        }

        Marshal.Copy(plaintextPtr, plaintext, 0, (int) plaintextLen);

        return plaintext;
    }

    public void Dispose() => ctx.Dispose();

    public static SealProvider Create(EnvelopeContext ctx) => new SealProvider(ctx);
}
