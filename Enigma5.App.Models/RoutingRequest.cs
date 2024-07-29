using System.Buffers.Text;
using Enigma5.App.Models.Contracts;

namespace Enigma5.App.Models;

public class RoutingRequest: IValidatable
{
    public string? Payload { get; set; }

    public RoutingRequest(string payload)
    {
        Payload = payload;
    }

    public RoutingRequest() { }

    public IEnumerable<Error> Validate()
    {
        if(string.IsNullOrWhiteSpace(Payload))
        {
            yield return new Error(ValidationErrors.NULL_REQUIRED_PROPERTIES, [nameof(Payload)]);
        }

        if(Payload is not null && !Base64.IsValid(Payload))
        {
            yield return new Error(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, [nameof(Payload)]);
        }
    }
}
