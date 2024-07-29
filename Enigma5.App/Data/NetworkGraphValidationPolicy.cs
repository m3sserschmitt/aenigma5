﻿using Enigma5.Crypto;
using System.Text.Json;
using Enigma5.App.Models.Extensions;

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

    public static bool ValidatePublicKey(this Vertex vertex)
    => vertex.PublicKey.IsValidPublicKey();

    public static bool ValidateAddress(this Vertex vertex)
    => vertex.Neighborhood.Address.IsValidAddress()
    && CertificateHelper.GetHexAddressFromPublicKey(vertex.PublicKey) == vertex.Neighborhood.Address;

    public static bool ValidateNeighborsAddresses(this Vertex vertex)
    => vertex.Neighborhood.Neighbors.All(address => address.IsValidAddress());

    public static bool ValidatePolicy(this Vertex vertex)
    => vertex.ValidatePublicKey()
    && vertex.ValidateAddress()
    && vertex.CheckCycles()
    && vertex.ValidateNeighborsAddresses()
    && vertex.ValidateSignature();
}
