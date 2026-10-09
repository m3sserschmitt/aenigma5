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
using Enigma5.Crypto;

namespace Enigma5.Tests.Base;

public static class TestSignatures
{
    // What a client sends to sign in: the challenge of the node, signed with the client's key.
    public static string SignChallenge(string privateKey, string challenge)
    {
        var encrypted = privateKey == TestKeys.PrivateKey1 || privateKey == TestKeys.PrivateKey2;
        using var signer = SealProvider.Factory.CreateSigner(privateKey, encrypted ? Encoding.UTF8.GetBytes(TestKeys.Passphrase + "\0") : null);
        return Convert.ToBase64String(signer.Sign(Convert.FromBase64String(challenge)) ?? throw new InvalidOperationException("The test key could not sign."));
    }
}
