using Enigma5.Crypto;
using Enigma5.App.Common.Extensions;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Enigma5.App.Data;

public static partial class NetworkGraphValidationPolicy
{
    public static bool ValidateSignature(this Vertex vertex)
    {
        if(vertex.SignedData is null)
        {
            return false;
        }

        try
        {
            var decodedSignature = Convert.FromBase64String(vertex.SignedData);
            var plaintext = decodedSignature.GetStringDataFromSignature();

            if(plaintext is null)
            {
                return false;
            }

            var expectedNeighborhood = JsonSerializer.Deserialize<Neighborhood>(plaintext);

            if(vertex.Neighborhood != expectedNeighborhood)
            {
                return false;
            }

            using var envelope = Envelope.Factory.CreateSignatureVerification(vertex.PublicKey!);

            return envelope.Verify(decodedSignature);
        }
        catch
        {
            return false;
        }
    }

    public static bool CheckCycles(this Vertex vertex)
    => !vertex.Neighborhood.Neighbors.Contains(vertex.Neighborhood.Address);

    public static bool ValidateAddress(this Vertex vertex)
    => vertex.PublicKey is not null && CertificateHelper.GetHexAddressFromPublicKey(vertex.PublicKey) == vertex.Neighborhood.Address;

    public static bool ValidateNeighborsAddresses(this Vertex vertex)
    => vertex.Neighborhood.Neighbors.All(address => AddressRegex().IsMatch(address));

    public static bool ValidatePolicy(this Vertex vertex)
    => ValidateAddress(vertex)
    && CheckCycles(vertex)
    && ValidateNeighborsAddresses(vertex)
    && ValidateSignature(vertex);

    [GeneratedRegex("^[a-fA-F0-9]{64}$")]
    private static partial Regex AddressRegex();
}
