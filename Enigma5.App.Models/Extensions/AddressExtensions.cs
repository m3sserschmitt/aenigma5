using System.Text.RegularExpressions;

namespace Enigma5.App.Models.Extensions;

public static partial class AddressExtensions
{
    public static bool IsValidAddress(this string? address)
    => !string.IsNullOrWhiteSpace(address) && AddressRegex().IsMatch(address);

    [GeneratedRegex(@"^[a-f0-9]{64}$")]
    private static partial Regex AddressRegex();
}
