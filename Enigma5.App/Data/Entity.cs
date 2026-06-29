using System.ComponentModel.DataAnnotations;

namespace Enigma5.App.Data;

public class Entity
{
    [Required]
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
