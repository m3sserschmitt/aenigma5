using System.ComponentModel.DataAnnotations;

namespace Enigma5.App.Data;

public class SharedData(string data)
{
    [Key]
    public string Tag { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string Data { get; set; } = data;

    [Required]
    public DateTime DateCreated { get; set; } = DateTime.Now;
}
