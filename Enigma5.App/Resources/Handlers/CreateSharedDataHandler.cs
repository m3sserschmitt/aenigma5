using Enigma5.App.Common.Constants;
using Enigma5.App.Common.Extensions;
using Enigma5.App.Resources.Commands;
using Enigma5.Crypto;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Resources.Handlers;

public class CreateSharedDataHandler(
    Data.EnigmaDbContext context,
    IConfiguration configuration)
    : IRequestHandler<CreateSharedDataCommand, CommandResult<Models.SharedData>>
{
    private readonly Data.EnigmaDbContext _context = context;

    private readonly IConfiguration _configuration = configuration;

    public async Task<CommandResult<Models.SharedData>> Handle(CreateSharedDataCommand request, CancellationToken cancellationToken)
    {
        if (request.SharedDataCreate.PublicKey is null || request.SharedDataCreate.SignedData is null)
        {
            return CommandResult.CreateResultFailure<Models.SharedData>();
        }

        using var signatureVerification = Envelope.Factory.CreateSignatureVerification(request.SharedDataCreate.PublicKey);

        if (signatureVerification is null)
        {
            return CommandResult.CreateResultFailure<Models.SharedData>();
        }

        var decodedSignature = Convert.FromBase64String(request.SharedDataCreate.SignedData);

        if (decodedSignature is null || decodedSignature.Length == 0 || !signatureVerification.Verify(decodedSignature))
        {
            return CommandResult.CreateResultFailure<Models.SharedData>();
        }

        var sharedData = new Data.SharedData(request.SharedDataCreate.SignedData, request.SharedDataCreate.AccessCount);
        await _context.AddAsync(sharedData, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        var hostname = _configuration.GetHostname();

        return CommandResult.CreateResultSuccess(new Models.SharedData
        {
            Tag = sharedData.Tag,
            ResourceUrl = hostname is not null ? $"{hostname}/{Endpoints.ShareEndpoint}?Tag={sharedData.Tag}" : null,
            ValidUntil = DateTimeOffset.Now + DataPersistencePeriod.SharedDataPersistancePeriod,
            Data = sharedData.Data
        });
    }
}
