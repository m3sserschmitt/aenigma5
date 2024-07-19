using MediatR;

namespace Enigma5.App.Resources.Queries;

public class CheckAuthorizedServiceQuery(string address)
: IRequest<bool>
{
    public string Address { get; set; } = address;
}
