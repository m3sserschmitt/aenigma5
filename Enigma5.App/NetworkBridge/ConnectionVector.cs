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
using Enigma5.App.Common.Extensions;
using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Models.Contracts.Hubs;
using Enigma5.App.Models.HubInvocation;
using Enigma5.Security.Contracts;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net;

namespace Enigma5.App.NetworkBridge;

public class ConnectionVector : IDisposable
{
    private bool _disposed;

    private bool _isReversed;

    private bool _sourceAuthenticated;

    private bool _targetAuthenticated;

    private string? _sourceAddress;

    private string? _targetAddress;

    private readonly string? _impersonateServiceAddress;

    private readonly string _sourceHubHost;

    private readonly string _targetHubHost;

    private readonly HubConnection _source;

    private readonly HubConnection _target;

    private readonly NetworkGraphValidationPolicy _networkGraphValidationPolicy;

    private readonly ICertificateManager _certificateManager;

    private readonly ILogger _logger;

    public string? SourceAddress
    {
        get => _sourceAddress;
        private set => _sourceAddress = value;
    }

    public string? TargetAddress
    {
        get => _targetAddress;
        private set => _targetAddress = value;
    }

    public string? ImpersonateServiceAddress
    {
        get => _impersonateServiceAddress;
    }

    public bool IsReversed
    {
        get => _isReversed;
        private set => _isReversed = value;
    }

    public bool SourceAuthenticated
    {
        get => _sourceAuthenticated;
        private set => _sourceAuthenticated = value;
    }

    public bool TargetAuthenticated
    {
        get => _targetAuthenticated;
        private set => _targetAuthenticated = value;
    }

    public bool Authenticated => Connected && SourceAuthenticated && TargetAuthenticated;

    public bool Connected => _source.State == HubConnectionState.Connected && _target.State == HubConnectionState.Connected;

    public event Func<Exception?, ConnectionVector, Task>? Closed;

    private ConnectionVector(
        string sourceUrl,
        string targetUrl,
        string? impersonateServiceAddress,
        NetworkGraphValidationPolicy networkGraphValidationPolicy,
        ICertificateManager certificateManager,
        IConfiguration configuration,
        ILogger logger)
    {
        _source = CreateHubConnection(sourceUrl, options =>
        {
            if (!string.IsNullOrWhiteSpace(impersonateServiceAddress))
            {
                options.Headers.Add(Constants.XImpersonateServiceHeaderKey, impersonateServiceAddress);
            }
        });
        _target = CreateHubConnection(targetUrl, options =>
        {
            if (targetUrl.IsValidOnionUrl())
            {
                var socks5ProxyAddress = configuration.GetSocks5Proxy();
                if (string.IsNullOrWhiteSpace(socks5ProxyAddress))
                {
                    return;
                }
                var handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(socks5ProxyAddress),
                    UseProxy = true
                };
                options.HttpMessageHandlerFactory = _ => handler;
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            }
        });
        _certificateManager = certificateManager;
        _networkGraphValidationPolicy = networkGraphValidationPolicy;

        var sourceUri = new Uri(sourceUrl);
        _sourceHubHost = $"{sourceUri.Host}:{sourceUri.Port}";
        var targetUri = new Uri(targetUrl);
        _targetHubHost = $"{targetUri.Host}:{targetUri.Port}";

        _source.Closed += OnSourceClosed;
        _target.Closed += OnTargetClosed;

        _impersonateServiceAddress = impersonateServiceAddress;
        _logger = logger;
    }

    private ConnectionVector(ConnectionVector connectionVector, bool reversed)
    {
        _source = reversed ? connectionVector._target : connectionVector._source;
        _target = reversed ? connectionVector._source : connectionVector._target;
        _sourceHubHost = reversed ? connectionVector._targetHubHost : connectionVector._sourceHubHost;
        _targetHubHost = reversed ? connectionVector._sourceHubHost : connectionVector._targetHubHost;
        _isReversed = reversed;
        _certificateManager = connectionVector._certificateManager;
        _impersonateServiceAddress = connectionVector._impersonateServiceAddress;
        _networkGraphValidationPolicy = connectionVector._networkGraphValidationPolicy;
        _logger = connectionVector._logger;
    }

    ~ConnectionVector()
    {
        Dispose(false);
    }

    public void TargetOn<T>(string method, Func<T, Task> handler)
    => _target.On(method, handler);

    public void SourceOn<T>(string method, Func<T, Task> handler)
    => _source.On(method, handler);

    public async Task<bool> InvokeTargetAsync(string method, object? data, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug($"Invoking target {{{Constants.Serilog.HubMethodNameKey}}} on connection vector {{{Constants.Serilog.ConnectionVectorKey}}} with the following {{@{Constants.Serilog.HubMethodArgumentsKey}}}.", method, this, data);
        return await InvokeAsync(_target, method, data, cancellationToken);
    }

    public async Task<bool> InvokeSourceAsync(string method, object? data, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug($"Invoking source {{{Constants.Serilog.HubMethodNameKey}}} on connection vector {{{Constants.Serilog.ConnectionVectorKey}}} with the following {{@{Constants.Serilog.HubMethodArgumentsKey}}}.", method, this, data);
        return await InvokeAsync(_source, method, data, cancellationToken);
    }

    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug($"Invoking {{{Constants.Serilog.ConnectionVectorMethodNameKey}}} on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", nameof(StartAsync), this);
            if (_target.State == HubConnectionState.Disconnected)
            {
                _logger.LogDebug($"Starting target connection for connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", this);
                await _target.StartAsync(cancellationToken);
            }
            else
            {
                _logger.LogDebug($"Target already started for connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", this);
            }
            
            if (_source.State == HubConnectionState.Disconnected)
            {
                _logger.LogDebug($"Starting source connection for connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", this);
                await _source.StartAsync(cancellationToken);
            }
            else
            {
                _logger.LogDebug($"Source already started for connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", this);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Exception encountered while invoking {{{Constants.Serilog.ConnectionVectorMethodNameKey}}} on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", nameof(StartAsync), this);
            await OnTargetClosed(ex);
            return false;
        }
    }

    public async Task<bool> StopAsync(CancellationToken cancellationToken = default)
    => await StopAsync(_target, cancellationToken) && await StopAsync(_source, cancellationToken);

    public async Task<bool> StartAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug($"Invoking {{{Constants.Serilog.ConnectionVectorMethodNameKey}}} for connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", nameof(StartAuthenticationAsync), this);
        if (Authenticated)
        {
            _logger.LogDebug($"Connection vector {{{Constants.Serilog.ConnectionVectorKey}}} already authenticated. Skipping...", this);
            return true;
        }

        if (!Connected)
        {
            _logger.LogError($"Connection vector {{{Constants.Serilog.ConnectionVectorKey}}} was not properly started. Aborting...", this);
            await StopAsync(cancellationToken);
            return false;
        }

        if (!IsReversed && TargetAuthenticated)
        {
            var reversedVector = Reversed();
            SourceAuthenticated = await reversedVector.StartAuthenticationAsync(cancellationToken);
            SourceAddress = reversedVector.TargetAddress;

            return Authenticated || (await StopAsync(cancellationToken) && false);
        }

        try
        {
            if (!await RequestTargetVertexAsync(cancellationToken))
            {
                _logger.LogError($"Requesting target vertex failed for connection vector {{{Constants.Serilog.ConnectionVectorKey}}}. Aborting...", this);
                await StopAsync(cancellationToken);
                return false;
            }

            var nonce = await _target.InvokeAsync<InvocationResultDto<string>>(nameof(IEnigmaHub.GenerateToken), cancellationToken);

            if (!nonce.Success || nonce.Data is null)
            {
                _logger.LogError($"Invalid nonce returned from target on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}", this);
                await StopAsync(cancellationToken);
                return false;
            }

            var signature = await SignNonceAsync(nonce.Data);

            if (signature is null)
            {
                _logger.LogError($"Signing nonce failed for connection vector {{{Constants.Serilog.ConnectionVectorKey}}}", this);
                await StopAsync(cancellationToken);
                return false;
            }

            var authentication = await _target.InvokeAsync<InvocationResultDto<bool>>(
                nameof(IEnigmaHub.Authenticate),
                new AuthenticationRequestDto(signature.PublicKey, signature.SignedData),
                cancellationToken: cancellationToken);
            TargetAuthenticated = authentication.Success && authentication.Data;

            if (TargetAuthenticated)
            {
                _logger.LogDebug($"Target authenticated for connection vector {{{Constants.Serilog.ConnectionVectorKey}}}", this);
            }
            else
            {
                _logger.LogError($"Authentication failed for connection vector {{{Constants.Serilog.ConnectionVectorKey}}}", this);
            }

            if (!IsReversed && TargetAuthenticated)
            {
                var reversedVector = Reversed();
                SourceAuthenticated = await reversedVector.StartAuthenticationAsync(cancellationToken);
                SourceAddress = reversedVector.TargetAddress;

                return Authenticated || (await StopAsync(cancellationToken) && false);
            }

            return TargetAuthenticated || (await StopAsync(cancellationToken) && false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error encountered while invoking {{{Constants.Serilog.ConnectionVectorMethodNameKey}}} on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", nameof(StartAuthenticationAsync), this);
            await StopAsync(cancellationToken);
            return false;
        }
    }

    private async Task<SignatureDto?> SignNonceAsync(string nonce)
    {
        try
        {
            _logger.LogDebug($"Invoking {{{Constants.Serilog.ConnectionVectorMethodNameKey}}} for connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", nameof(SignNonceAsync), this);

            var publicKey = await _certificateManager.GetPublicKeyAsync();
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                _logger.LogError($"Public key not available while invoking {{{Constants.Serilog.ConnectionVectorMethodNameKey}}} on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", nameof(SignNonceAsync), this);
                return null;
            }

            var decodedNonce = Convert.FromBase64String(nonce);
            if (decodedNonce is null)
            {
                _logger.LogError($"Nonce could not be base64 decoded while invoking {{{Constants.Serilog.ConnectionVectorMethodNameKey}}} on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", nameof(SignNonceAsync), this);
                return null;
            }

            using var signer = await _certificateManager.CreateSignerAsync();
            var data = signer.Sign(decodedNonce);

            if (data == null)
            {
                _logger.LogError($"Data could not be signed while invoking {{{Constants.Serilog.ConnectionVectorMethodNameKey}}} on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", nameof(SignNonceAsync), this);
                return null;
            }

            var encodedData = Convert.ToBase64String(data);

            if (encodedData is null)
            {
                _logger.LogError($"Signed data could not be base64 encoded while invoking {{{Constants.Serilog.ConnectionVectorMethodNameKey}}} on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", nameof(SignNonceAsync), this);
                return null;
            }

            return new(encodedData, publicKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while invoking {{{Constants.Serilog.ConnectionVectorMethodNameKey}}} on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", nameof(SignNonceAsync), this);
            return null;
        }
    }

    private async Task<bool> RequestTargetVertexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug($"Invoking {{{Constants.Serilog.ConnectionVectorMethodNameKey}}} on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", nameof(RequestTargetVertexAsync), this);
            var response = await _target.InvokeAsync<InvocationResultDto<Vertex>>(nameof(IEnigmaHub.GetLocalVertex), cancellationToken);
            var vertex = response.Data;
            if (vertex == null || !response.Success)
            {
                _logger.LogError($"Invalid response encountered while requesting target vertex on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", this);
                return false;
            }

            TargetAddress = vertex?.Neighborhood?.Address;
            if (!string.IsNullOrWhiteSpace(ImpersonateServiceAddress) && TargetAddress != ImpersonateServiceAddress)
            {
                if (TargetAddress != await _certificateManager.GetAddressAsync())
                {
                    _logger.LogError($"Unexpected target address received on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", this);
                    return false;
                }
            }

            return vertex != null && _networkGraphValidationPolicy.Validate(vertex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error encountered while invoking {{{Constants.Serilog.ConnectionVectorMethodNameKey}}} on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", nameof(RequestTargetVertexAsync), this);
            return false;
        }
    }

    private async Task OnSourceClosed(Exception? ex)
    {
        _logger.LogError(ex, $"Source closed on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", this);
        SourceAuthenticated = false;
        await StopAsync(_target);
    }

    private async Task OnTargetClosed(Exception? ex)
    {
        _logger.LogError(ex, $"Target closed on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", this);
        TargetAuthenticated = false;
        await StopAsync(_source);
        _ = Task.Run(() => Closed?.Invoke(ex, this));
    }

    private ConnectionVector Reversed() => new(this, true);

    private async Task<bool> InvokeAsync(HubConnection connection, string method, object? data, CancellationToken cancellationToken = default)
    {
        try
        {
            await connection.InvokeAsync(method, data, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Exception while invoking {{{Constants.Serilog.HubMethodNameKey}}} with the following data {{@{Constants.Serilog.HubMethodArgumentsKey}}}.", method, data);
            return false;
        }
    }

    private async Task<bool> StopAsync(HubConnection connection, CancellationToken cancellationToken = default)
    {
        try
        {
            if (connection.State != HubConnectionState.Disconnected)
            {
                await connection.StopAsync(cancellationToken);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while invoking {{{Constants.Serilog.HubMethodNameKey}}} on connection vector {{{Constants.Serilog.ConnectionVectorKey}}}.", nameof(StopAsync), this);
            return false;
        }
    }

    public static bool operator ==(ConnectionVector? vector1, ConnectionVector? vector2)
    {
        if (ReferenceEquals(vector1, vector2))
        {
            return true;
        }

        if (vector1 is null || vector2 is null)
        {
            return false;
        }

        return vector1._sourceHubHost == vector2._sourceHubHost && vector1._targetHubHost == vector2._targetHubHost;
    }

    public static bool operator !=(ConnectionVector vector1, ConnectionVector vector2) => !(vector1 == vector2);

    public override int GetHashCode() => HashCode.Combine(_sourceHubHost, _targetHubHost);

    public override bool Equals(object? obj) => Equals(obj as ConnectionVector);

    public bool Equals(ConnectionVector? other) => this == other;

    public static ConnectionVector Create(
        string sourceHubUrl,
        PeerDto peer,
        NetworkGraphValidationPolicy networkGraphValidationPolicy,
        ICertificateManager certificateManager,
        IConfiguration configuration,
        ILogger logger)
    => new(
        sourceHubUrl,
        (string.IsNullOrWhiteSpace(peer.Host) ? null : peer.Host)
        ?? throw new ArgumentException("Peer host could not be null or white spaces."),
        peer.Address,
        networkGraphValidationPolicy,
        certificateManager,
        configuration,
        logger);

    private static HubConnection CreateHubConnection(string baseUrl, Action<HttpConnectionOptions> httpOptions)
    => new HubConnectionBuilder().WithUrl(
        $"{baseUrl.Trim('/')}/{Constants.OnionRoutingEndpoint}",
        options => httpOptions(options)).Build();

    public static HubConnection CreateHubConnection(string baseUrl) => CreateHubConnection(baseUrl, x => { });

    public static HashSet<ConnectionVector> CreateConnections(
        string sourceUrl,
        List<PeerDto> peers,
        NetworkGraphValidationPolicy networkGraphValidationPolicy,
        ICertificateManager certificateManager,
        IConfiguration configuration,
        ILogger logger)
    => [.. peers.Where(item => !string.IsNullOrWhiteSpace(item.Host))
    .Select(item => Create(sourceUrl, item, networkGraphValidationPolicy, certificateManager, configuration, logger))
    ];

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {

            }
            _source.Closed -= OnSourceClosed;
            _target.Closed -= OnTargetClosed;
            _disposed = true;
        }
    }

    public override string ToString()
    => $"{_sourceHubHost} ({(string.IsNullOrWhiteSpace(_sourceAddress) ? "unknown address" : _sourceAddress)}) -> {_targetHubHost} ({(string.IsNullOrWhiteSpace(_targetAddress) ? "unknown address" : _targetAddress)})";
}
