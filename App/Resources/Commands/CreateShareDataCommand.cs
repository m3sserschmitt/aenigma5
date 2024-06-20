using MediatR;

namespace Enigma5.App.Resources.Commands;

public class CreateShareDataCommand(string signedData) : IRequest<string?>
{
    public string SignedData { get; set; } = signedData;
}
