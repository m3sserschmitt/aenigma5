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

using Enigma5.Security.Contracts;
using Enigma5.Crypto.DataProviders;
using System.Diagnostics.CodeAnalysis;

namespace Enigma5.Tests.Base;

[ExcludeFromCodeCoverage]
public class TestCertificateManager : ICertificateManager
{
    public string PublicKey => PKey.PublicKey3;

    public string PrivateKey => PKey.PrivateKey3;

    public string Address => PKey.Address3;
}
