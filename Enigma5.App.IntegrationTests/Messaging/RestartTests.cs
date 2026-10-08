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

namespace Enigma5.App.IntegrationTests.Messaging;

public class RestartTests
{
    [Fact]
    public async Task Stored_messages_are_still_there_after_the_node_is_ended_and_started_again()
    {
        await using var node = await NodeProcess.StartAAsync();
        await using (var sender = await HubClient.SignedInAsync(node.PublicUrl, TestKeys.PrivateKey2))
        {
            Assert.True((await sender.Route(null, TestOnions.ForClientThroughA1, TestOnions.ForClientThroughA2)).Success);
        }

        await node.RestartAsync();

        await using var recipient = await HubClient.SignedInAsync(node.PublicUrl, TestKeys.PrivateKey3);
        var page = await recipient.Pull2();
        Assert.Equal(2, page.Data!.Count);
        Assert.All(page.Data, message => Assert.Equal(TestKeys.Address3, message.Destination));
    }
}
