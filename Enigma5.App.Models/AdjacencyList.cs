using Enigma5.App.Models.Contracts;
using Enigma5.App.Models.Extensions;

namespace Enigma5.App.Models;

public partial class AdjacencyList : IValidatable
{
    public string? Address { get; set; }

    public string? Hostname { get; set; }

    public List<string>? Neighbors { get; set; }

    public IEnumerable<Error> Validate()
    {
        if (string.IsNullOrWhiteSpace(Address))
        {
            yield return new Error(ValidationErrors.NULL_REQUIRED_PROPERTIES, [nameof(Address)]);
        }

        if (Neighbors is null)
        {
            yield return new Error(ValidationErrors.NULL_REQUIRED_PROPERTIES, [nameof(Neighbors)]);
        }

        if (Address is not null && !Address.IsValidAddress())
        {
            yield return new Error(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, [nameof(Address)]);
        }

        if (Neighbors is not null && Neighbors.Any(item => !item.IsValidAddress()))
        {
            yield return new Error(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, [nameof(Neighbors)]);
        }
    }
}
