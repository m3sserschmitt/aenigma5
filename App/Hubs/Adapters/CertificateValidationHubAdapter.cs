using Enigma5.App.Hubs.Contracts;
using Enigma5.App.Hubs.Extensions;

using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs.Adapters;

public class CertificateValidationHubAdapter : ICertificateValidationHub
{
    private readonly ICertificateValidationHub? certificateValidationHub;

    public CertificateValidationHubAdapter(Hub hub)
    {
        certificateValidationHub = hub.As<ICertificateValidationHub>();
    }

    public string? Address
    {
        get => certificateValidationHub?.Address;
        set
        {
            if (certificateValidationHub != null)
            {
                certificateValidationHub.Address = value;
            }
        }
    }
}
