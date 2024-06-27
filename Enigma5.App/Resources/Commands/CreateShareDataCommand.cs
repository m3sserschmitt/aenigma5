using MediatR;

namespace Enigma5.App.Resources.Commands;

public class CreateShareDataCommand(string signedData, int accessCount = 1) : IRequest<string?>
{
    public string SignedData { get; set; } = signedData;

    public int AccessCount { get; set; } = accessCount;
}
