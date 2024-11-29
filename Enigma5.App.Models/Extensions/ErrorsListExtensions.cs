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

namespace Enigma5.App.Models.Extensions;

public static class ErrorsListExtensions
{
    public static void AddError(this HashSet<Error> errors, string message, string? property = null)
    {
        if(property is not null && errors.TryGetValue(new Error(message), out Error? actualError) && actualError.Properties is not null)
        {
            actualError.Properties.Add(property);
        }
        else if(property is not null)
        {
            errors.Add(new Error(message, [ property ]));
        }
        else
        {
            errors.Add(new Error(message));
        }
    }

    public static void AddErrors(this HashSet<Error> errors, HashSet<Error> errorsToAdd)
    {
        foreach(var errorToAdd in errorsToAdd)
        {
            if(errorToAdd.Message is null)
            {
                continue;
            }
            if(errorToAdd.Properties?.Count == 0)
            {
                errors.AddError(errorToAdd.Message);
                continue;
            }
            foreach(var property in errorToAdd.Properties ?? [])
            {
                errors.AddError(errorToAdd.Message, property);
            }
        }
    }
}
