using Enigma5.App.Models.Contracts;
using Enigma5.App.Models.Extensions;

namespace Enigma5.App.Models;

public class TriggerBroadcastRequest : IValidatable
{
    public List<string>? NewAddresses { get; set; }

    public TriggerBroadcastRequest(List<string> newAddresses)
    {
        NewAddresses = newAddresses;
    }

    public TriggerBroadcastRequest()
    {
        NewAddresses = [];
    }

    public IEnumerable<Error> Validate()
    {
        if(NewAddresses?.Any(item => !item.IsValidAddress()) ?? false)
        {
            yield return new Error(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, [nameof(NewAddresses)]);
        }
    }
}
