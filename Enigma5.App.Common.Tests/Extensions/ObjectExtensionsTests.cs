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

using Enigma5.App.Common.Extensions;

namespace Enigma5.App.Common.Tests.Extensions;

public class ObjectExtensionsTests
{
    private sealed class Sample
    {
        public string? Zulu { get; set; }

        public int Alpha { get; set; }

        public string? Missing { get; set; }

        public List<string> Items { get; set; } = [];
    }

    [Fact]
    public void CopyBySerialization_returns_an_equal_object_that_is_not_the_same()
    {
        var source = new Sample { Zulu = "z", Alpha = 1, Items = ["one", "two"] };

        var copy = source.CopyBySerialization();

        Assert.NotSame(source, copy);
        Assert.NotSame(source.Items, copy.Items);
        Assert.Equal("z", copy.Zulu);
        Assert.Equal(1, copy.Alpha);
        Assert.Equal(source.Items, copy.Items);
    }

    [Fact]
    public void CanonicallySerialize_orders_the_properties_and_leaves_out_null_values()
    {
        var source = new Sample { Zulu = "z", Alpha = 1, Items = ["one"] };

        Assert.Equal("{\"Alpha\":1,\"Items\":[\"one\"],\"Zulu\":\"z\"}", source.CanonicallySerialize());
    }

    [Fact]
    public void CanonicallySerialize_gives_the_same_text_for_equal_objects()
    {
        var first = new Sample { Zulu = "z", Alpha = 1 };
        var second = new Sample { Alpha = 1, Zulu = "z" };

        Assert.Equal(first.CanonicallySerialize(), second.CanonicallySerialize());
    }
}
