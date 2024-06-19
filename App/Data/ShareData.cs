using System.ComponentModel.DataAnnotations;

namespace Enigma5.App.Data;

public class ShareData(string data)
{
    [Key]
    public Guid Tag { get; set; } = Guid.NewGuid();

    [Required]
    public string Data { get; set; } = data;

    [Required]
    public DateTime DateCreated { get; set; } = DateTime.Now;
}
