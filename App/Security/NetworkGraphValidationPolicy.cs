using Enigma5.App.Data;
using Enigma5.Crypto;
using Enigma5.App.Common.Extensions;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Enigma5.App.Security;

public static class NetworkGraphValidationPolicy
{
    public static bool ValidateSignature(Vertex vertex)
    {
        try
        {
            var decodedSignature = Convert.FromBase64String(vertex.SignedData!);
            var adjacencyList = decodedSignature.GetStringDataFromSignature();
            
            if(JsonSerializer.Serialize(vertex.Neighborhood) != adjacencyList)
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

    public static bool CheckCycles(Vertex vertex)
    {
        if(vertex.Neighborhood.Neighbors.Contains(vertex.Neighborhood.Address))
        {
            return false;
        }

        return true;
    }

    public static bool ValidateAddress(Vertex vertex)
    {
        if(CertificateHelper.GetHexAddressFromPublicKey(vertex.PublicKey) != vertex.Neighborhood.Address)
        {
            return false;
        }

        return true;
    }

    public static bool ValidateNeighborsAddresses(Vertex vertex)
    {
        foreach(var address in vertex.Neighborhood.Neighbors)
        {
            if(!Regex.IsMatch(address, "^[a-fA-F0-9]{64}$"))
            {
                return false;
            }
        }
        
        return true;
    }

    public static bool Validate(Vertex vertex)
    => ValidateAddress(vertex)
    && CheckCycles(vertex)
    && ValidateNeighborsAddresses(vertex)
    && ValidateSignature(vertex);
}
