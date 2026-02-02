using System.Text;

namespace Enigma5.Crypto.Extensions;

public static class ByteArrayExtensions
{
    public static byte[]? GetDataFromSignature(this byte[]? signature, string publicKey)
    {
        if (signature == null)
        {
            return null;
        }

        var digestLength = SealProvider.GetPKeySize(publicKey);

        if (signature.Length < digestLength + 1)
        {
            return null;
        }

        return signature[..^digestLength];
    }

    public static string? GetStringDataFromSignature(this byte[]? signature, string publicKey)
    {
        try
        {
            var data = signature.GetDataFromSignature(publicKey);

            if (data == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(data);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
