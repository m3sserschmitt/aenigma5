/*
    Aenigma - Federal messaging system
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
using Enigma5.Crypto.DataProviders;
using Enigma5.Structures.DataProviders.Contracts;
using Enigma5.Crypto;

namespace Enigma5.Structures.DataProviders;

public class TestOnionPeel : ITestOnion
{
    private IOnion onion;

    public TestOnionPeel(ITestOnion testOnion)
    {
        ExpectedNextAddress = PKey.Address2;
        ExpectedContent = (byte[])testOnion.Content.Clone();

        onion = OnionBuilder
            .Create(testOnion)
            .AddPeel()
            .SetNextAddress(HashProvider.FromHexString(ExpectedNextAddress))
            .Seal(PKey.ServerPublicKey)
            .Build();
    }

    public string ExpectedNextAddress { get; set; }

    public byte[] ExpectedContent { get; set; }

    public byte[] Content { get => onion.Content; set => onion.Content = value; }
}
