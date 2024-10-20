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

namespace Enigma5.App.Common.Utils;

public static class ThreadSafeExecution
{
    public delegate T Func<T, U>(out U a);

    public static T Execute<T>(Func<T> action, T defaultReturn, object locker)
    {
        T result = defaultReturn;

        lock (locker)
        {
            result = action();
        }

        return result;
    }

    public static T Execute<T, U>(Func<T, U> action, T defaultReturn, out U outParam, object locker)
    {
        T result = defaultReturn;

        lock(locker)
        {
            result = action(out outParam);
        }

        return result;
    }
}
