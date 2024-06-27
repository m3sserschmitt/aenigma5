using System.Text;

namespace Enigma5.App.Common.Extensions;

public static class SignatureExtensions
{
    public static byte[]? GetDataFromSignature(this byte[]? signature)
    {
        if (signature == null)
        {
            return null;
        }

        var digestLength = Crypto.Constants.DefaultPKeySize / 8;

        if (signature.Length < digestLength + 1)
        {
            return null;
        }

        return signature[..^digestLength];
    }

    public static string? GetStringDataFromSignature(this byte[]? signature)
    {
        var data = signature.GetDataFromSignature();

        if (data == null)
        {
            return null;
        }

        return Encoding.UTF8.GetString(data);
    }
}
