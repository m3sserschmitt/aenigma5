using Enigma5.App.Models.Contracts;
using Enigma5.App.Models.Extensions;

namespace Enigma5.App.Models;

public class RoutingRequestBulkDto(List<string?>? payloads = null): IValidatable
{
    public List<string?>? Payloads { get; private set; } = payloads;

    public HashSet<ErrorDto> Validate()
    {
        var errors = new HashSet<ErrorDto>();
        if(Payloads is null)
        {
            errors.AddError(ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(Payloads));
        }
        if(Payloads?.Any(string.IsNullOrWhiteSpace) ?? false)
        {
            errors.AddError(ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(Payloads));
        }
        
        return errors;
    }
}
