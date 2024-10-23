using System.Buffers.Text;

namespace Enigma5.App.Models.Extensions;

public static class Base64Extensions
{
    public static bool IsValidBase64(this string? data) => data is not null && Base64.IsValid(data);
}
