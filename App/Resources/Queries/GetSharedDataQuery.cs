using Enigma5.App.Data;
using MediatR;

namespace Enigma5.App.Resources.Queries;

public class GetSharedDataQuery(string tag) : IRequest<SharedData?>
{
    public string Tag { get; set; } = tag;
}
