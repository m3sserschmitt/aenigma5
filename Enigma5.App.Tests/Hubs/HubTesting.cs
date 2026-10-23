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

using Enigma5.App.Models.HubInvocation;
using Enigma5.App.Resources.Commands;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Tests.Hubs;

// What the tests of the hub methods share.
internal static class HubTesting
{
    public const string Connection = "connection-1";

    public const string OtherConnection = "connection-2";

    public static string SingleError<T>(InvocationResultDto<T> result)
    {
        Assert.False(result.Success);
        return Assert.Single(result.Errors).Message!;
    }

    // Stores messages for a destination, as RouteMessage does after it has removed the node's layer.
    public static async Task Store(TestNode node, string destination, int count)
    {
        for (var index = 0; index < count; index++)
        {
            var content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"content {index}"));
            Assert.True((await node.Send(new CreatePendingMessageCommand(destination, content, null))).Success);
        }
    }

    public static Task<int> Confirmed(TestNode node) => node.Database(context => context.Messages.CountAsync(message => message.Sent));
}
