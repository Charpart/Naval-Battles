using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NavalBattles.Runtime.Client;
using NavalBattles.Runtime.Client.Identity;
using NavalBattles.Runtime.Domain.Configuration;
using NavalBattles.Runtime.Transport.Diagnostics;
using NavalBattles.Runtime.Transport.Simulation;
using NavalBattles.Runtime.UnityIntegration.Configuration;
using NavalBattles.Runtime.UnityIntegration.Fusion;

namespace NavalBattles.Runtime.UnityIntegration.Bootstrap
{
    public sealed class GameBootstrap
    {
        private const int CLIENT_SIMULATION_SEED = 101;

        private readonly GameRules _rules;
        private readonly int _seed;
        private readonly GameProcessRole _role;
        private readonly SessionSettings _sessionSettings;
        private readonly IClientIdentityStore _clientIdentity;
        private readonly ClientNetworkSettings _clientNetworkSettings;
        private readonly IElapsedTimeProvider _timeProvider;
        private readonly FusionPeerFactory _peerFactory = new FusionPeerFactory();
        private ServerRuntime _serverRuntime;
        private ClientRuntime _clientRuntime;
        private UniTaskCompletionSource _recreationCompletion;
        private bool _hasStarted;
        private double _currentTime;

        public GameProcessRole role => _role;
        public GameClient client => _clientRuntime?.client;
        public ulong serverRevision => _serverRuntime?.server.stateRevision ?? 0;
        public int connectedPlayerCount => _serverRuntime?.server.connectedPlayerCount ?? 0;
        public double currentTime => _currentTime;

        public event Action<TransportLogEntry> messageLogged;

        public GameBootstrap(
            GameBootstrapSettings settings,
            GameProcessRole role,
            IClientIdentityStore clientIdentity = null,
            IElapsedTimeProvider timeProvider = null)
        {
            settings = settings ?? throw new ArgumentNullException(nameof(settings));

            if (role != GameProcessRole.Server && role != GameProcessRole.Client)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(role),
                    role,
                    "A game process must run as Server or Client.");
            }

            _rules = settings.rules;
            _seed = settings.seed;
            _role = role;
            _sessionSettings = settings.session;
            _clientNetworkSettings = settings.clientNetwork;
            _timeProvider = timeProvider ?? new StopwatchElapsedTimeProvider();
            _clientIdentity = role == GameProcessRole.Client
                ? clientIdentity ?? throw new ArgumentNullException(nameof(clientIdentity))
                : null;
        }

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            if (_hasStarted)
                throw new InvalidOperationException("The game has already been started.");

            _hasStarted = true;

            try
            {
                if (_role == GameProcessRole.Server)
                {
                    await StartServerAsync(cancellationToken);
                }
                else
                {
                    _clientRuntime = await StartClientAsync(cancellationToken);
                    _clientRuntime.messageLogged += OnMessageLogged;
                }
            }
            catch
            {
                await ShutdownAsync(CancellationToken.None);
                _hasStarted = false;
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        public void Tick()
        {
            _currentTime = _timeProvider.elapsedSeconds;
            _serverRuntime?.Tick(_currentTime);
            _clientRuntime?.Tick(_currentTime);
        }

        public async UniTask ShutdownAsync(CancellationToken cancellationToken)
        {
            UniTaskCompletionSource recreationCompletion = _recreationCompletion;

            if (recreationCompletion != null)
                await recreationCompletion.Task;

            await ShutdownClientAsync();
            if (_serverRuntime != null)
            {
                await _serverRuntime.ShutdownAsync(CancellationToken.None);
                _serverRuntime = null;
            }

            _hasStarted = false;
            cancellationToken.ThrowIfCancellationRequested();
        }

        public async UniTask RecreateClientAsync(CancellationToken cancellationToken)
        {
            if (_role != GameProcessRole.Client)
                return;

            if (_recreationCompletion != null)
                throw new InvalidOperationException("Client recreation is already in progress.");

            var completion = new UniTaskCompletionSource();
            _recreationCompletion = completion;

            try
            {
                await ShutdownClientAsync();
                cancellationToken.ThrowIfCancellationRequested();
                _clientRuntime = await StartClientAsync(cancellationToken);
                _clientRuntime.messageLogged += OnMessageLogged;
            }
            finally
            {
                _recreationCompletion = null;
                completion.TrySetResult();
            }
        }

        public void SetConnected(bool isConnected)
        {
            _clientRuntime?.SetConnected(isConnected);
            if (isConnected)
                _clientRuntime?.Connect();
        }

        public void SetNetworkSimulationSettings(NetworkSimulationSettings settings)
        {
            _clientRuntime?.SetNetworkSimulationSettings(settings);
        }

        private async UniTask StartServerAsync(CancellationToken cancellationToken)
        {
            FusionPeer peer = await _peerFactory.StartServerAsync(_sessionSettings, cancellationToken);

            try
            {
                _serverRuntime = new ServerRuntime(peer, _rules, _seed, _timeProvider);
            }
            catch
            {
                await peer.ShutdownAsync(CancellationToken.None);
                throw;
            }
        }

        private async UniTask<ClientRuntime> StartClientAsync(CancellationToken cancellationToken)
        {
            FusionPeer peer = await _peerFactory.StartClientAsync(_sessionSettings, cancellationToken);
            ClientRuntime runtime = null;

            try
            {
                runtime = new ClientRuntime(
                    peer,
                    _clientIdentity,
                    _clientNetworkSettings,
                    CLIENT_SIMULATION_SEED);
                _currentTime = _timeProvider.elapsedSeconds;
                runtime.Tick(_currentTime);
                runtime.Connect();

                return runtime;
            }
            catch
            {
                if (runtime != null)
                {
                    await runtime.ShutdownAsync(CancellationToken.None);
                }
                else
                {
                    await peer.ShutdownAsync(CancellationToken.None);
                }
                throw;
            }
        }

        private async UniTask ShutdownClientAsync()
        {
            ClientRuntime runtime = _clientRuntime;
            _clientRuntime = null;

            if (runtime == null)
                return;

            runtime.messageLogged -= OnMessageLogged;
            await runtime.ShutdownAsync(CancellationToken.None);
        }

        private void OnMessageLogged(TransportLogEntry entry)
        {
            messageLogged?.Invoke(entry);
        }
    }
}
