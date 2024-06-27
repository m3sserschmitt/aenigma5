using System.ComponentModel.DataAnnotations;

namespace Enigma5.App.Data;

public class SharedData(string data, int maxAccessCount = 1)
{
    [Key]
    public string Tag { get; set; } = Guid.NewGuid().ToString();

    public string Data { get; set; } = data;

    public int AccessCount { get; set; }

    public int MaxAccessCount { get; set; } = maxAccessCount;

    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.Now;
}
