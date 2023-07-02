using Microsoft.AspNetCore.SignalR;

using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Contracts;
using Enigma5.Message;
using Enigma5.App.Hubs.Extensions;
using Enigma5.App.Security;

namespace Enigma5.App.Hubs.Filters;

public class OnionParsingFilter : BaseFilter<IOnionParsingHub, OnionParsingAttribute>
{
    private readonly CertificateManager certificateManager;

    public OnionParsingFilter(CertificateManager certificateManager)
    {
        this.certificateManager = certificateManager;
    }

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var data = invocationContext.MethodInvocationArgument<string>(0);
        if (data != null)
        {
            using (var onionParser = OnionParser.Factory.Create(certificateManager.PrivateKey, certificateManager.Passphrase))
            {
                var decodedData = Convert.FromBase64String(data);
                if (onionParser.Parse(new Onion { Content = decodedData }))
                {
                    var hub = new OnionParsingHubAdapter(invocationContext.Hub);

                    hub.Content = onionParser.Content;
                    hub.Next = onionParser.NextAddress;
                    hub.Size = onionParser.Size;
                }
            }
        }

        return await next(invocationContext);
    }
}
