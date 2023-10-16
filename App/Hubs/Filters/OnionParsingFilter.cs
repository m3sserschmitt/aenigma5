using Microsoft.AspNetCore.SignalR;

using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Contracts;
using Enigma5.Message;
using Enigma5.App.Hubs.Extensions;
using Enigma5.App.Security;

namespace Enigma5.App.Hubs.Filters;

public class OnionParsingFilter : BaseFilter<IOnionParsingHub, OnionParsingAttribute>
{
    private readonly CertificateManager _certificateManager;

    public OnionParsingFilter(CertificateManager certificateManager)
    {
        _certificateManager = certificateManager;
    }

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var data = invocationContext.MethodInvocationArgument<string>(0);
        if (data != null)
        {
            using var onionParser = OnionParser.Factory.Create(_certificateManager.PrivateKey, string.Empty);
            var decodedData = Convert.FromBase64String(data);
            if (onionParser.Parse(new Onion { Content = decodedData }))
            {
                _ = new OnionParsingHubAdapter(invocationContext.Hub)
                {
                    Content = onionParser.Content,
                    Next = onionParser.NextAddress,
                    Size = onionParser.Size
                };
            }
        }

        return await next(invocationContext);
    }
}
