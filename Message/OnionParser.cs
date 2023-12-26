using Enigma5.Core;
using Enigma5.Crypto;
using Enigma5.Crypto.Contracts;
using Enigma5.Message.Contracts;

namespace Enigma5.Message;

public class OnionParser : IDisposable
{
    private readonly IEnvelopeUnseal _unseal;

    public int Size { get; private set; }

    public byte[]? Next { get; private set; }

    public string? NextAddress { get; private set; }

    public byte[]? Content { get; private set; }

    private OnionParser(IEnvelopeUnseal unseal)
    {
        _unseal = unseal;
    }

    ~OnionParser()
    {
        _unseal.Dispose();
    }

    public void Dispose()
    {
        _unseal.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Reset()
    {
        Size = 0;
        Next = null;
        NextAddress = null;
        Content = null;
    }

    public bool Parse(IOnion onion)
    {
        var size = DecodeSize(new ArraySegment<byte>(onion.Content, 0, 2).ToArray());

        if (onion.Content.Length - 2 != size)
        {
            return false;
        }

        var envelope = new ArraySegment<byte>(onion.Content, 2, size).ToArray();
        var decryptedData = _unseal.Unseal(envelope);

        if (decryptedData == null)
        {
            return false;
        }

        Size = size;
        Next = new ArraySegment<byte>(decryptedData, 0, AddressContext.Current.AddressSize).ToArray();
        NextAddress = HashProvider.ToHex(Next);
        Content = new ArraySegment<byte>(
            decryptedData,
            AddressContext.Current.AddressSize,
            decryptedData.Length - AddressContext.Current.AddressSize
            ).ToArray();

        return true;
    }

    public static ushort DecodeSize(byte[] size)
    {
        if (size.Length != 2)
        {
            throw new ArgumentException("Invalid buffer length. Expected 2 bytes.");
        }

        return (ushort)(size[0] * 256 + size[1]);
    }

    public static class Factory
    {
        public static OnionParser Create(string key, string passphrase)
        {
            return new OnionParser(Envelope.Factory.CreateUnseal(key, passphrase));
        }

        public static OnionParser CreateFromFile(string path, string passphrase)
        {
            return new OnionParser(Envelope.Factory.CreateUnsealFromFile(path, passphrase));
        }
    }
}
