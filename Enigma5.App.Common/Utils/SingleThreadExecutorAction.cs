/*
    Aenigma - Onion Routing based messaging application
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

public class SingleThreadExecutorAction(Action action)
{
    private readonly Action _noReturnValueAction = action;

    public Exception? Exception { get; protected set; }

    public virtual bool HasReturnValue => false;

    public virtual void Invoke()
    {
        try
        {
            _noReturnValueAction.Invoke();
        }
        catch (Exception ex)
        {
            Exception = ex;
        }
    }

    public static implicit operator SingleThreadExecutorAction(Action action)
    {
        return new SingleThreadExecutorAction(action);
    }
}

public class SingleThreadExecutorAction<T>(Func<T> action) : SingleThreadExecutorAction(() => { })
{
    private readonly Func<T> _actionWithReturnValue = action;

    public T? Result { get; private set; }

    public override bool HasReturnValue => true;

    public override void Invoke()
    {
        try
        {
            Result = _actionWithReturnValue.Invoke();
        }
        catch (Exception ex)
        {
            Exception = ex;
        }
    }

    public static implicit operator SingleThreadExecutorAction<T>(Func<T> action)
    {
        return new SingleThreadExecutorAction<T>(action);
    }
}
