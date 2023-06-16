using Microsoft.AspNetCore.SignalR;

using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Contracts;
using Enigma5.Message;
using Enigma5.App.Hubs.Extensions;
using Enigma5.Crypto;

namespace Enigma5.App.Hubs.Filters;

public class OnionParserFilter : BaseFilter<IOnionParserHub, OnionParserAttribute>
{
    private readonly CertificateManager certificateManager;

    public OnionParserFilter(CertificateManager certificateManager)
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
                    var hub = new OnionParserHubAdapter(invocationContext.Hub);

                    hub.Content = onionParser.Content;
                    hub.Next = HashProvider.Sha256(onionParser.Next!);
                    hub.Size = onionParser.Size;
                }
            }
        }

        return await next(invocationContext);
    }
}
