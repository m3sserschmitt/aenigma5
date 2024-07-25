namespace Enigma5.App.Models;

public class Error
{
    public string? Message { get; set; }

    public IEnumerable<string> Properties { get; set; }

    public Error(string message, IEnumerable<string> properties)
    {
        Message = message;
        Properties = properties;
    }

    public Error()
    {
        Properties = [];
    }
}
