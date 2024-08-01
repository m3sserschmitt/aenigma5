using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace NetworkBridge;

public class ConnectionVector
{
    private readonly object _locker = new();

    private bool _isReversed;

    private bool _sourceAuthenticated;

    private bool _targetAuthenticated;

    private readonly HubConnection _source;

    private readonly HubConnection _target;

    public string? SourcePublicKey { get; private set; }

    public string? TargetPublicKey { get; private set; }

    public bool IsReversed
    {
        get
        {
            lock (_locker)
            {
                return _isReversed;
            }
        }
        set
        {
            lock (_locker)
            {
                _isReversed = value;
            }
        }
    }

    public bool SourceAuthenticated
    {
        get
        {
            lock (_locker)
            {
                return _sourceAuthenticated;
            }
        }
        set
        {
            lock (_locker)
            {
                _sourceAuthenticated = value;
            }
        }
    }

    public bool TargetAuthenticated
    {
        get
        {
            lock (_locker)
            {
                return _targetAuthenticated;
            }
        }
        set
        {
            lock (_locker)
            {
                _targetAuthenticated = value;
            }
        }
    }

    public bool Authenticated => Connected && SourceAuthenticated && TargetAuthenticated;

    public bool Connected => _source.State == HubConnectionState.Connected && _target.State == HubConnectionState.Connected;

    public event Func<Exception?, Task>? TargetClosed
    {
        add
        {
            _target.Closed += value;
        }
        remove
        {
            _target.Closed -= value;
        }
    }

    public event Func<Exception?, Task>? SourceClosed
    {
        add
        {
            _source.Closed += value;
        }
        remove
        {
            _source.Closed -= value;
        }
    }

    public ConnectionVector(HubConnection source, HubConnection target)
    {
        _source = source;
        _target = target;

        _source.Closed += OnSourceClosed;
        _target.Closed += OnTargetClosed;
    }

    public void TargetOn<T>(string method, Func<T, Task> handler)
    => _target.On(method, handler);

    public void SourceOn<T>(string method, Func<T, Task> handler)
    => _source.On(method, handler);

    public async Task<bool> InvokeTargetAsync(string method, object? data)
    => await InvokeAsync(_target, method, data);

    public async Task<bool> InvokeSourceAsync(string method, object? data)
    => await InvokeAsync(_source, method, data);

    public async Task<bool> InvokeTargetAsync(string method)
    => await InvokeAsync(_target, method);

    public async Task<bool> InvokeSourceAsync(string method)
    => await InvokeAsync(_source, method);

    public async Task<bool> StopTargetAsync()
    => await StopAsync(_target);

    public async Task<bool> StopSourceAsync()
    => await StopAsync(_source);

    public async Task<bool> StartAsync()
    {
        try
        {
            if (_source.State == HubConnectionState.Disconnected)
            {
                await _source.StartAsync();
            }

            if (_target.State == HubConnectionState.Disconnected)
            {
                await _target.StartAsync();
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> StopAsync()
    => await StopAsync(_target) && await StopAsync(_source);

    public async Task<bool> StartAuthenticationAsync()
    {
        if (Authenticated)
        {
            return true;
        }

        if (!Connected)
        {
            return false;
        }

        if (!IsReversed && TargetAuthenticated)
        {
            var reversedVector = Reversed();
            SourceAuthenticated = await reversedVector.StartAuthenticationAsync();
            TargetPublicKey = reversedVector.SourcePublicKey;

            return Authenticated;
        }

        try
        {
            var nonce = await _target.InvokeAsync<InvocationResult<string>>(nameof(IHub.GenerateToken));

            if (!nonce.Success || nonce.Data is null)
            {
                return false;
            }

            var signature = await _source.InvokeAsync<InvocationResult<Signature>>(nameof(IHub.SignToken), new SignatureRequest(nonce.Data));

            if (!signature.Success || signature.Data is null)
            {
                return false;
            }

            var authentication = await _target.InvokeAsync<InvocationResult<bool>>(nameof(IHub.Authenticate), new AuthenticationRequest
            {
                Signature = signature.Data.SignedData,
                PublicKey = signature.Data.PublicKey,
                SyncMessagesOnSuccess = false
            });

            TargetAuthenticated = authentication.Success && authentication.Data;
            SourcePublicKey = signature.Data.PublicKey;

            if (!IsReversed && TargetAuthenticated)
            {
                var reversedVector = Reversed();
                SourceAuthenticated = await reversedVector.StartAuthenticationAsync();
                TargetPublicKey = reversedVector.SourcePublicKey;
                
                return Authenticated;
            }

            return TargetAuthenticated;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private ConnectionVector Reversed() => new(_target, _source) { IsReversed = true };

    private static async Task<bool> InvokeAsync(HubConnection connection, string method, object? data)
    {
        try
        {
            await connection.InvokeAsync(method, data);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task<bool> InvokeAsync(HubConnection connection, string method)
    {
        try
        {
            await connection.InvokeAsync(method);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task<bool> StopAsync(HubConnection connection)
    {
        try
        {
            if (connection.State != HubConnectionState.Disconnected)
            {
                await connection.StopAsync();
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private Task OnSourceClosed(Exception? _)
    {
        SourceAuthenticated = false;
        return Task.CompletedTask;
    }

    private Task OnTargetClosed(Exception? _)
    {
        TargetAuthenticated = false;
        return Task.CompletedTask;
    }
}
