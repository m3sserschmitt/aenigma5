using Enigma5.App.Models.Contracts;
using Enigma5.App.Models.Extensions;

namespace Enigma5.App.Models;

public class RoutingRequestBulk(List<string?>? payloads = null): IValidatable
{
    public List<string?>? Payloads { get; private set; } = payloads;

    public HashSet<Error> Validate()
    {
        var errors = new HashSet<Error>();
        if(Payloads is null)
        {
            errors.AddError(ValidationErrors.NULL_REQUIRED_PROPERTIES, nameof(Payloads));
        }
        if(Payloads?.Any(string.IsNullOrWhiteSpace) ?? false)
        {
            errors.AddError(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(Payloads));
        }
        
        return errors;
    }
}
