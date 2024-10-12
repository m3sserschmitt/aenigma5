/*
    Aenigma - Onion Routing based messaging application
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

using Enigma5.Structures.Contracts;
using Enigma5.Structures.DataProviders.Contracts;
using Enigma5.Crypto.DataProviders;
using Enigma5.Crypto;
using System.Text;

namespace Enigma5.Structures.DataProviders;

public class TestOnion : ITestOnion
{
    private IOnion onion;

    public TestOnion(ISetMessageContent builder)
    {
        ExpectedContent = Encoding.UTF8.GetBytes("Test Onion");
        ExpectedNextAddress = PKey.Address2;
        new Random().NextBytes(ExpectedContent);

        onion = builder
            .SetMessageContent(ExpectedContent)
            .SetNextAddress(HashProvider.FromHexString(ExpectedNextAddress))
            .Seal(PKey.PublicKey2)
            .Build();
    }

    public string ExpectedNextAddress { get; set; }

    public byte[] ExpectedContent { get; set; }

    public byte[] Content { get => onion.Content; set => onion.Content = value; }
}
