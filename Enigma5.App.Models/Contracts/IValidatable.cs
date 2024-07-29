namespace Enigma5.App.Models.Contracts;

public interface IValidatable
{
    public IEnumerable<Error> Validate();
}
