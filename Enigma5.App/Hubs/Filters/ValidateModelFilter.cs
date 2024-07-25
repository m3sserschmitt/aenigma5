using Enigma5.App.Attributes;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Hubs.Extensions;
using Enigma5.App.Models;
using Enigma5.App.Models.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs.Filters;

public class ValidateModelFilter : BaseFilter<IHub, ValidateModelAttribute>
{
    protected override bool CheckArguments(HubInvocationContext invocationContext)
    => invocationContext.HubMethodArguments.Count == 1 && invocationContext.HubMethodArguments[0] is IValidatable;

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        // TODO: refactor this to support any number of arguments;
        var data = invocationContext.MethodInvocationArgument<IValidatable>(0);

        if(data is null)
        {
            return new InvocationResult<object?>(default, []);
        }

        var errors = data.Validate().ToList();

        if(errors.Count != 0)
        {
            return new InvocationResult<object?>(default, errors);
        }

        return await next(invocationContext);
    }
}
