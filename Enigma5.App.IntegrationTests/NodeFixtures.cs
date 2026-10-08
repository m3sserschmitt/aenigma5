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

namespace Enigma5.App.IntegrationTests;

// One node for all tests of a class. xUnit runs the tests of one class one after the other.
public class NodeAFixture : IAsyncLifetime
{
    public NodeProcess Node { get; private set; } = null!;

    protected virtual (string Key, string Value)[] Settings => [];

    public async Task InitializeAsync() => Node = await NodeProcess.StartAAsync(Settings);

    public async Task DisposeAsync() => await Node.DisposeAsync();
}

// Node A with small limits for shared data and files, so that the tests reach them with little data.
public class SmallLimitsNodeFixture : NodeAFixture
{
    public const int SharedDataMaxSize = 2048;

    public const int SharedFileMaxSize = 4096;

    protected override (string Key, string Value)[] Settings =>
    [
        ("SharedDataMaxSize", SharedDataMaxSize.ToString()),
        ("SharedFileMaxSize", SharedFileMaxSize.ToString())
    ];
}

// Node B connects to node A, as if A had been added on the dashboard of B. A does not connect to B.
public class TwoNodesFixture : IAsyncLifetime
{
    public NodeProcess A { get; private set; } = null!;

    public NodeProcess B { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        A = await NodeProcess.StartAAsync();
        B = await NodeProcess.StartBAsync();
        B.AddPeer(A);
        await B.RestartAsync();
    }

    public async Task DisposeAsync()
    {
        await B.DisposeAsync();
        await A.DisposeAsync();
    }
}
