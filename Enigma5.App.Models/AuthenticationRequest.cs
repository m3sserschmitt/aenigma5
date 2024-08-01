using System.Buffers.Text;
using Enigma5.App.Models.Contracts;
using Enigma5.App.Models.Extensions;

namespace Enigma5.App.Models;

public class AuthenticationRequest: IValidatable
{
    public string? PublicKey { get; set; }

    public string? Signature { get; set; }

    public bool SyncMessagesOnSuccess { get; set; }

    public IEnumerable<Error> Validate()
    {
        if(string.IsNullOrWhiteSpace(PublicKey))
        {
            yield return new Error(ValidationErrors.NULL_REQUIRED_PROPERTIES, [nameof(PublicKey)]);
        }

        if(string.IsNullOrWhiteSpace(Signature))
        {
            yield return new Error(ValidationErrors.NULL_REQUIRED_PROPERTIES, [nameof(Signature)]);
        }

        if(PublicKey is not null && !PublicKey.IsValidPublicKey())
        {
            yield return new Error(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, [nameof(PublicKey)]);
        }

        if(Signature is not null && !Base64.IsValid(Signature))
        {
            yield return new Error(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, [nameof(Signature)]);
        }
    }
}
