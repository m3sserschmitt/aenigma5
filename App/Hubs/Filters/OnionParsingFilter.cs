using Microsoft.AspNetCore.SignalR;
using Enigma5.App.Attributes;
using Enigma5.Message;
using Enigma5.App.Hubs.Extensions;
using Enigma5.App.Security;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Hubs.Adapters;

namespace Enigma5.App.Hubs.Filters;

public class OnionParsingFilter(OnionParsingService decryptionService) : BaseFilter<IOnionParsingHub, OnionParsingAttribute>
{
    private readonly OnionParsingService _decryptionService = decryptionService;

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var data = invocationContext.MethodInvocationArgument<string>(0);
        if (data != null)
        {
            var decodedData = Convert.FromBase64String(data);
            if (_decryptionService.Parse(new Onion { Content = decodedData }))
            {
                _ = new OnionParsingHubAdapter(invocationContext.Hub)
                {
                    Content = _decryptionService.Content,
                    Next = _decryptionService.NextAddress,
                    Size = _decryptionService.Size
                };
            }

            _decryptionService.Reset();
        }

        return await next(invocationContext);
    }
}
