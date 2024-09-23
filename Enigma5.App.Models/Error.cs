namespace Enigma5.App.Models;

public class Error
{
    public string? Message { get; set; }

    public IEnumerable<string> Properties { get; set; }

    public Error(string message, List<string> properties)
    {
        Message = message;
        Properties = properties;
    }

    public Error(string message)
    {
        Message = message;
        Properties = [];
    }

    public Error()
    {
        Properties = [];
    }
}
