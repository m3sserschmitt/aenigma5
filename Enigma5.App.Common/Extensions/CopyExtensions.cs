using System.Text.Json;

namespace Enigma5.App.Common.Extensions;

public static class CopyExtensions
{
    public static T CopyBySerialization<T>(this T source) where T : class
    {
        var serializedData = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<T>(serializedData)!;
    }
}
