using Enigma5.Message;
using Enigma5.Message.Contracts;

namespace Enigma5.App.Security;

public class OnionParsingService(CertificateManager certificateManager): IDisposable
{
    private readonly OnionParser _onionParser = OnionParser.Factory.Create(certificateManager.PrivateKey, string.Empty);

    ~OnionParsingService()
    {
        _onionParser.Dispose();
    }

    public int Size => _onionParser.Size;

    public byte[]? Next => _onionParser.Next;

    public string? NextAddress => _onionParser.NextAddress;

    public byte[]? Content => _onionParser.Content;

    public void Dispose()
    {
        _onionParser.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Reset() => _onionParser.Reset();

    public bool Parse(IOnion onion) => _onionParser.Parse(onion);
}
