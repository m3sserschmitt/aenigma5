using System.Buffers.Text;
using Enigma5.App.Models.Contracts;

namespace Enigma5.App.Models;

public class SignatureRequest: IValidatable
{
    public string? Nonce { get; set; }

    public SignatureRequest(string nonce)
    {
        Nonce = nonce;
    }

    public SignatureRequest() { }

    public IEnumerable<Error> Validate()
    {
        if(string.IsNullOrWhiteSpace(Nonce))
        {
            yield return new Error(ValidationErrors.NULL_REQUIRED_PROPERTIES, [nameof(Nonce)]);
        }

        if(Nonce is not null && !Base64.IsValid(Nonce))
        {
            yield return new Error(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, [nameof(Nonce)]);
        }
    }
}
