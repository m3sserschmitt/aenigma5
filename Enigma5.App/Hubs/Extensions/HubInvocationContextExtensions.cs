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

using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs.Extensions;

public static class HubInvocationContextExtensions
{
    public static T? MethodInvocationArgument<T>(this HubInvocationContext invocationContext, int index)
    where T : class
    {
        try
        {
            return invocationContext.HubMethodArguments[index] is T argument ? argument : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
