using System.Runtime.InteropServices;
using Enigma5.Core;
using Enigma5.Crypto;
using Enigma5.Crypto.Contracts;
using Enigma5.Message.Contracts;

namespace Enigma5.Message;

public class OnionParser : IDisposable
{
    private readonly IEnvelopeUnseal _unsealService;

    public int Size { get; private set; }

    public byte[]? Next { get; private set; }

    public string? NextAddress { get; private set; }

    public byte[]? Content { get; private set; }

    private OnionParser(IEnvelopeUnseal unsealService)
    {
        _unsealService = unsealService;
    }

    ~OnionParser()
    {
        _unsealService.Dispose();
    }

    public void Dispose()
    {
        _unsealService.Dispose();
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
        var data = _unsealService.UnsealOnion(onion.Content, out int outLen);

        if(data == IntPtr.Zero || outLen < 0)
        {
            return false;
        }

        var contentLen = outLen - AddressContext.Current.AddressSize;

        Next = new byte[AddressContext.Current.AddressSize];
        Content = new byte[contentLen];

        Marshal.Copy(data, Next, 0, AddressContext.Current.AddressSize);
        Marshal.Copy(data + AddressContext.Current.AddressSize, Content, 0, contentLen);
        NextAddress = HashProvider.ToHex(Next);

        return true;
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
