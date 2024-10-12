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

using System.Collections.Concurrent;

namespace Enigma5.App.Common.Utils;

public class SingleThreadExecutor<T>
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly BlockingCollection<SingleThreadExecutorAction> _actions = [];

    private readonly BlockingCollection<SingleThreadExecutorResult<T>> _returnValues = [];

    private readonly Task _looper;

    public SingleThreadExecutor()
    {
        _looper = new Task(() =>
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var action = _actions.Take();
                    action.Invoke();

                    if (action.HasReturnValue && action is SingleThreadExecutorAction<T> actionWithReturnValue)
                    {
                        _returnValues.Add(new SingleThreadExecutorResult<T>(actionWithReturnValue.Result, actionWithReturnValue.Exception));
                    }
                    else if (!action.HasReturnValue && action is SingleThreadExecutorAction actionWithNoReturnValue)
                    {
                        _returnValues.Add(new SingleThreadExecutorResult<T>(default, actionWithNoReturnValue.Exception));
                    }
                }
                catch (Exception ex)
                {
                    _returnValues.Add(new SingleThreadExecutorResult<T>(default, ex));
                }
            }
        });
    }

    public void StartLooper()
    {
        _looper.Start();
    }

    public void Cancel()
    {
        var dummyAction = new Action(() => { });
        _cancellationTokenSource.Cancel();
        _actions.Add(dummyAction);
    }

    public SingleThreadExecutorResult<T> Execute(Func<T> action)
    {
        try
        {
            _actions.Add((SingleThreadExecutorAction<T>)action);
            return _returnValues.Take();
        }
        catch (Exception ex)
        {
            return new SingleThreadExecutorResult<T>(default, ex);
        }
    }

    public Exception? Execute(Action action)
    {
        try
        {

            _actions.Add((SingleThreadExecutorAction)action);
            return _returnValues.Take().Exception;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
