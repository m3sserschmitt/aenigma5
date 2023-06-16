using Microsoft.AspNetCore.SignalR;

using Enigma5.App.Hubs.Adapters;
using Enigma5.App.Hubs.Extensions;
using Enigma5.App.Attributes;
using Enigma5.App.Contracts;
using Enigma5.Crypto;
using Enigma5.App.Hubs.Contracts;

namespace Enigma5.App.Hubs.Filters;

public class CertificateValidationFilter : BaseFilter<ICertificateValidationHub, CertificateValidationAttribute>
{
    private readonly IConnectionsMapper connectionsMapper;

    public CertificateValidationFilter(IConnectionsMapper connectionsMapper)
    {
        this.connectionsMapper = connectionsMapper;
    }

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var certificate = invocationContext.MethodInvocationArgument<string>(0);
        var hub = new CertificateValidationHubAdapter(invocationContext.Hub)
        {
            Address = certificate != null ? CertificateHelper.ValidateSelfSignedCertificate(certificate) : null
        };        

        if (hub.Address != null)
        {
            connectionsMapper.TryAdd(hub.Address, invocationContext.Hub.Context.ConnectionId);
        }

        return await next(invocationContext);
    }
}
