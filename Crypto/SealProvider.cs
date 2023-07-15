using Enigma5.Core;
using Enigma5.Crypto.Contracts;
using System.Runtime.InteropServices;

namespace Enigma5.Crypto;

public sealed class SealProvider :
    IEnvelopeSeal,
    IEnvelopeUnseal,
    IEnvelopeSign,
    IEnvelopeVerify
{
    private EnvelopeContext ctx;

    private SealProvider(EnvelopeContext ctx)
    {
        this.ctx = ctx;
    }

    [DllImport("cryptography")]
    private static extern IntPtr EncryptData(IntPtr ctx, byte[] plaintext, uint plaintextLen);

    [DllImport("cryptography")]
    private static extern IntPtr DecryptData(IntPtr ctx, byte[] ciphertext, uint ciphertextLen);

    [DllImport("cryptography")]
    private static extern IntPtr SignData(IntPtr ctx, byte[] plaintext, uint plaintextLen);

    [DllImport("cryptography")]
    private static extern bool VerifySignature(IntPtr ctx, byte[] ciphertext, uint ciphertextLen);

    [DllImport("cryptography")]
    private static extern uint GetEnvelopeSize(uint pkeySizeBits, uint plaintextLen);

    [DllImport("cryptography")]
    private static extern uint GetOpenEnvelopeSize(uint pkeySizeBits, uint envelopeSize);

    public static int GetEnvelopeSize(int plaintextLen)
    => (int)GetEnvelopeSize((uint)PKeyContext.Current.PKeySize, (uint)plaintextLen);

    public static int GetOpenEnvelopeSize(int envelopeSize)
    => (int)GetOpenEnvelopeSize((uint)PKeyContext.Current.PKeySize, (uint)envelopeSize);

    public static int GetSignedDataSize(int plaintextLen)
    => plaintextLen + PKeyContext.Current.PKeySize / 8;

    private byte[]? Execute(byte[] input, Func<IntPtr, byte[], uint, IntPtr> encryptionProvider, Func<int, int> sizeComputer)
    {
        IntPtr outputPtr = encryptionProvider(ctx, input, (uint)input.Length);

        if (outputPtr == IntPtr.Zero)
        {
            return null;
        }

        var outLen = sizeComputer(input.Length);
        var output = new byte[outLen];

        Marshal.Copy(outputPtr, output, 0, outLen);

        return output;
    }

    public byte[]? Seal(byte[] plaintext)
    => Execute(plaintext, EncryptData, GetEnvelopeSize);

    public byte[]? Unseal(byte[] ciphertext)
    => Execute(ciphertext, DecryptData, GetOpenEnvelopeSize);

    public byte[]? Sign(byte[] plaintext)
    => Execute(plaintext, SignData, GetSignedDataSize);

    public bool Verify(byte[] ciphertext)
    => VerifySignature(ctx, ciphertext, (uint)ciphertext.Length);
    
    public void Dispose() => ctx.Dispose();

    public static SealProvider Create(EnvelopeContext ctx) => new SealProvider(ctx);
}
