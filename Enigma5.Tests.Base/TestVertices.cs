/*
    Aenigma - Federated messaging system
    Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

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

using System.Text;
using Enigma5.App.Common.Extensions;
using Enigma5.App.Models;
using Enigma5.Crypto;

namespace Enigma5.Tests.Base;

public static class TestVertices
{
    // The content a node signs: the same properties, in the same canonical form, as the node's own vertices.
    private sealed record Content(string Address, string? Hostname, DateTimeOffset LastUpdate, IEnumerable<string> Neighbors, string? OnionService);

    // A vertex for the owner of a test key, signed now, as that owner would send it to a node.
    public static VertexBroadcastRequestDto Signed(string privateKey, params string[] neighbors)
    {
        var publicKey = TestKeys.PublicKeyOf(privateKey);
        var content = new Content(CertificateHelper.GetHexAddressFromPublicKey(publicKey), null, DateTimeOffset.UtcNow, neighbors.Order(), null);
        var encrypted = privateKey == TestKeys.PrivateKey1 || privateKey == TestKeys.PrivateKey2;
        using var signer = SealProvider.Factory.CreateSigner(privateKey, encrypted ? Encoding.UTF8.GetBytes(TestKeys.Passphrase + "\0") : null);
        var signed = signer.Sign(Encoding.ASCII.GetBytes(content.CanonicallySerialize())) ?? throw new InvalidOperationException("The test key could not sign.");
        return new VertexBroadcastRequestDto(publicKey, Convert.ToBase64String(signed));
    }
}
