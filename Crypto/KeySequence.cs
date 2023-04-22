using System.Collections;
using System.Security.Cryptography;

namespace Crypto;

public class KeySequence : IEnumerable
{
    private byte[][] Keys { get; set; }

    public int Count { get; private set; }

    public KeySequence(int size, int keySize)
    {
        Keys = KeySequence.GenerateByteSequences(size, keySize);
        Count = size;
    }

    public static byte[][] GenerateByteSequences(int length, int keySize)
    {
        byte[][] sequences = new byte[length][];

        for (int i = 0; i < length; i++)
        {
            byte[] sequence = new byte[keySize];
            RandomNumberGenerator.Fill(sequence);
            sequences[i] = sequence;
        }
        return sequences;
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return Keys[i];
        }
    }
}
