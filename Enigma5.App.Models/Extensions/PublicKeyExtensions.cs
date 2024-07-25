using System.Text.RegularExpressions;

namespace Enigma5.App.Models.Extensions;

public static partial class PublicKeyExtensions
{
    public static bool IsValidPublicKey(this string? publicKey)
    => !string.IsNullOrWhiteSpace(publicKey) && PublicKeyRegex().IsMatch(publicKey);

    [GeneratedRegex(@"-+BEGIN PUBLIC KEY-+\s*([A-Za-z0-9+/=\s]+)-+END PUBLIC KEY-+", RegexOptions.Multiline)]
    private static partial Regex PublicKeyRegex();
}
