/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

using Enigma5.App.Common;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Models;
using Enigma5.App.Models.HubInvocation;
using Microsoft.AspNetCore.SignalR.Client;

namespace Enigma5.App.NetworkBridge;

internal class ConnectionVector
{
    private readonly object _locker = new();

    private bool _isReversed;

    private bool _sourceAuthenticated;

    private bool _targetAuthenticated;

    private string? _sourcePublicKey;

    private string? _targetPublicKey;

    internal readonly string SourceHubHost;

    internal readonly string TargetHubHost;

    private readonly HubConnection _source;

    private readonly HubConnection _target;

    internal string? SourcePublicKey
    {
        get
        {
            lock (_locker)
            {
                return _sourcePublicKey;
            }
        }
        private set
        {
            lock (_locker)
            {
                _sourcePublicKey = value;
            }
        }
    }

    internal string? TargetPublicKey
    {
        get
        {
            lock (_locker)
            {
                return _targetPublicKey;
            }
        }
        private set
        {
            lock (_locker)
            {
                _targetPublicKey = value;
            }
        }
    }

    internal bool IsReversed
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

    internal bool SourceAuthenticated
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

    internal bool TargetAuthenticated
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

    internal bool Authenticated => Connected && SourceAuthenticated && TargetAuthenticated;

    internal bool Connected => _source.State == HubConnectionState.Connected && _target.State == HubConnectionState.Connected;

    internal event Func<Exception?, Task>? TargetClosed
    {
        add
        {
            _target.Closed -= value;
            _target.Closed += value;
        }
        remove
        {
            _target.Closed -= value;
        }
    }

    internal event Func<Exception?, Task>? SourceClosed
    {
        add
        {
            _source.Closed -= value;
            _source.Closed += value;
        }
        remove
        {
            _source.Closed -= value;
        }
    }

    private ConnectionVector(HubConnection source, HubConnection target, string sourceUrl, string targetUrl)
    {
        _source = source;
        _target = target;

        var sourceUri = new Uri(sourceUrl);
        SourceHubHost = $"{sourceUri.Host}:{sourceUri.Port}";
        var targetUri = new Uri(targetUrl);
        TargetHubHost = $"{targetUri.Host}:{targetUri.Port}";

        _source.Closed += OnSourceClosed;
        _target.Closed += OnTargetClosed;
    }

    internal void TargetOn<T>(string method, Func<T, Task> handler)
    => _target.On(method, handler);

    internal void SourceOn<T>(string method, Func<T, Task> handler)
    => _source.On(method, handler);

    internal async Task<bool> InvokeTargetAsync(string method, object? data, CancellationToken cancellationToken = default)
    => await InvokeAsync(_target, method, data, cancellationToken);

    internal async Task<bool> InvokeSourceAsync(string method, object? data, CancellationToken cancellationToken = default)
    => await InvokeAsync(_source, method, data, cancellationToken);

    internal async Task<bool> InvokeTargetAsync(string method, CancellationToken cancellationToken = default)
    => await InvokeAsync(_target, method, cancellationToken);

    internal async Task<bool> InvokeSourceAsync(string method, CancellationToken cancellationToken = default)
    => await InvokeAsync(_source, method, cancellationToken);

    internal async Task<bool> StopTargetAsync(CancellationToken cancellationToken = default)
    => await StopAsync(_target, cancellationToken);

    internal async Task<bool> StopSourceAsync(CancellationToken cancellationToken = default)
    => await StopAsync(_source, cancellationToken);

    internal async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_source.State == HubConnectionState.Disconnected)
            {
                await _source.StartAsync(cancellationToken);
            }

            if (_target.State == HubConnectionState.Disconnected)
            {
                await _target.StartAsync(cancellationToken);
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    internal async Task<bool> StopAsync(CancellationToken cancellationToken = default)
    => await StopAsync(_target, cancellationToken) && await StopAsync(_source, cancellationToken);

    internal async Task<bool> StartAuthenticationAsync(CancellationToken cancellationToken = default)
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
            SourceAuthenticated = await reversedVector.StartAuthenticationAsync(cancellationToken);
            TargetPublicKey = reversedVector.SourcePublicKey;

            return Authenticated;
        }

        try
        {
            var nonce = await _target.InvokeAsync<InvocationResultDto<string>>(nameof(IEnigmaHub.GenerateToken), cancellationToken);

            if (!nonce.Success || nonce.Data is null)
            {
                return false;
            }

            var signature = await _source.InvokeAsync<InvocationResultDto<SignatureDto>>(nameof(IEnigmaHub.SignToken), new SignatureRequestDto(nonce.Data), cancellationToken: cancellationToken);

            if (!signature.Success || signature.Data is null)
            {
                return false;
            }

            var authentication = await _target.InvokeAsync<InvocationResultDto<bool>>(nameof(IEnigmaHub.Authenticate), new AuthenticationRequestDto(signature.Data.PublicKey, signature.Data.SignedData), cancellationToken: cancellationToken);

            TargetAuthenticated = authentication.Success && authentication.Data;
            SourcePublicKey = signature.Data.PublicKey;

            if (!IsReversed && TargetAuthenticated)
            {
                var reversedVector = Reversed();
                SourceAuthenticated = await reversedVector.StartAuthenticationAsync(cancellationToken);
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

    private ConnectionVector Reversed() => new(_target, _source, TargetHubHost, SourceHubHost) { IsReversed = true };

    private static async Task<bool> InvokeAsync(HubConnection connection, string method, object? data, CancellationToken cancellationToken = default)
    {
        try
        {
            await connection.InvokeAsync(method, data, cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task<bool> InvokeAsync(HubConnection connection, string method, CancellationToken cancellationToken = default)
    {
        try
        {
            await connection.InvokeAsync(method, cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task<bool> StopAsync(HubConnection connection, CancellationToken cancellationToken = default)
    {
        try
        {
            if (connection.State != HubConnectionState.Disconnected)
            {
                await connection.StopAsync(cancellationToken);
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool operator ==(ConnectionVector vector1, ConnectionVector vector2)
    => vector1.SourceHubHost == vector2.SourceHubHost && vector1.TargetHubHost == vector2.TargetHubHost;

    public static bool operator !=(ConnectionVector vector1, ConnectionVector vector2) => !(vector1 == vector2);

    public override int GetHashCode() => HashCode.Combine(SourceHubHost, TargetHubHost);

    public override bool Equals(object? obj) => obj is ConnectionVector vector && this == vector;

    internal static ConnectionVector Create(string sourceHubUrl, string targetHubUrl)
    => new(CreateHubConnection(sourceHubUrl), CreateHubConnection(targetHubUrl), sourceHubUrl, targetHubUrl);

    internal static HubConnection CreateHubConnection(string baseUrl)
    => new HubConnectionBuilder()
        .WithUrl($"{baseUrl.Trim('/')}/{Constants.OnionRoutingEndpoint}")
        .Build();

    internal static HashSet<ConnectionVector> CreateConnections(string sourceHubUrl, List<string> targetUrls)
    => targetUrls.Select(item => Create(sourceHubUrl, item)).ToHashSet();
}
