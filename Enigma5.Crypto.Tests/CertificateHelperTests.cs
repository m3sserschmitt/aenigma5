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

using Enigma5.Tests.Base;

namespace Enigma5.Crypto.Tests;

public class CertificateHelperTests
{
    public static TheoryData<string, string> Keys => new()
    {
        { TestKeys.PublicKey1, TestKeys.Address1 },
        { TestKeys.PublicKey2, TestKeys.Address2 },
        { TestKeys.PublicKey3, TestKeys.Address3 }
    };

    [Theory]
    [MemberData(nameof(Keys))]
    public void GetHexAddressFromPublicKey_gives_the_address_of_the_key(string publicKey, string address)
    {
        Assert.Equal(address, CertificateHelper.GetHexAddressFromPublicKey(publicKey));
    }

    [Fact]
    public void GetAddressFromPublicKey_gives_the_same_address_as_32_bytes()
    {
        var address = CertificateHelper.GetAddressFromPublicKey(TestKeys.PublicKey1);

        Assert.NotNull(address);
        Assert.Equal(TestKeys.Address1, HashProvider.ToHex(address));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a key")]
    public void Text_that_is_not_a_public_key_has_no_address(string? publicKey)
    {
        Assert.Null(CertificateHelper.GetAddressFromPublicKey(publicKey));
        Assert.Equal(string.Empty, CertificateHelper.GetHexAddressFromPublicKey(publicKey));
    }

    [Fact]
    public void A_private_key_has_no_address()
    {
        Assert.Equal(string.Empty, CertificateHelper.GetHexAddressFromPublicKey(TestKeys.PrivateKey3));
    }
}
