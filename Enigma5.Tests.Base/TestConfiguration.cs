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

using Microsoft.Extensions.Configuration;

namespace Enigma5.Tests.Base;

public static class TestConfiguration
{
    // A configuration that holds only the given settings, for example Create(("VertexLifetime", "00:10:00")).
    public static IConfiguration Create(params (string Key, string? Value)[] settings)
    => new ConfigurationBuilder()
        .AddInMemoryCollection(settings.Select(item => new KeyValuePair<string, string?>(item.Key, item.Value)))
        .Build();
}
