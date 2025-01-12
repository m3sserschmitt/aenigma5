/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

using Enigma5.Crypto;
using System.Text.Json;
using Enigma5.Crypto.Extensions;

namespace Enigma5.App.Data;

public static partial class NetworkGraphValidationPolicy
{
    public static bool ValidateSignature(this Vertex vertex)
    {
        if(vertex.SignedData is null || vertex.PublicKey is null)
        {
            return false;
        }

        try
        {
            var decodedSignature = Convert.FromBase64String(vertex.SignedData);
            var plaintext = decodedSignature.GetStringDataFromSignature(vertex.PublicKey);

            if(plaintext is null)
            {
                return false;
            }

            var expectedNeighborhood = JsonSerializer.Deserialize<Neighborhood>(plaintext);

            if(vertex.Neighborhood != expectedNeighborhood)
            {
                return false;
            }

            using var envelope = SealProvider.Factory.CreateVerifier(vertex.PublicKey);

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
