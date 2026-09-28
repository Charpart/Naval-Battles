using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NavalBattles.Runtime.Client.Connection;
using NavalBattles.Runtime.Client.Identity;
using NavalBattles.Runtime.Transport.Diagnostics;
using NavalBattles.Runtime.Transport.Simulation;
using NavalBattles.Runtime.UnityIntegration.Configuration;
using NavalBattles.Runtime.UnityIntegration.Presentation.Presenters;
using NavalBattles.Runtime.UnityIntegration.Presentation.Views;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NavalBattles.Runtime.UnityIntegration.Bootstrap
{
    public sealed class GameSceneController : MonoBehaviour, IGameDebugActions
    {
        [SerializeField] private GameRulesConfig _gameRules;
        [SerializeField] private SessionConfig _session;
        [SerializeField] private NetworkSimulationConfig _networkSimulation;
        [SerializeField] private ClientRecoveryConfig _clientRecovery;
        [SerializeField] private GameView _gameView;
        [SerializeField] private DebugPanelView _debugPanelView;
        [SerializeField] private int _matchSeed = 42;

        private CancellationTokenSource _lifetime;
        private UniTaskCompletionSource _startupCompletion;
        private GameBootstrap _bootstrap;
        private IGamePresenter _gamePresenter;
        private DebugPanelPresenter _debugPresenter;
        private UniTaskCompletionSource _shutdownCompletion;
        private bool _isRecreatingClient;
        private bool _isShuttingDown;
        private bool _isStarted;

        private readonly ClientIdentityStore processClientIdentity = new ClientIdentityStore(Guid.NewGuid());
        
        private GameProcessRole role { get; set; }

        public event Action<TransportLogEntry> messageLogged;

        public bool isReady => _isStarted && (role == GameProcessRole.Server
            || _bootstrap?.client?.connectionState == ClientConnectionState.Connected);

        private void Start()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            string projectRoot = LocalInstanceRoleConfiguration.GetProjectRoot(Application.dataPath);

            if (GameProcessRoleResolver.TryResolve(
                Application.isEditor,
                arguments,
                projectRoot,
                out GameProcessRole resolvedRole,
                out string error) == false)
            {
                role = default;
                Debug.LogError(error, this);
                enabled = false;
                return;
            }

            role = resolvedRole;
            ConfigureUi();
            StartGameAsync().Forget();
        }

        private void Update()
        {
            if (_isShuttingDown || _bootstrap == null)
                return;

            _bootstrap.Tick();
            _gamePresenter?.Refresh(_bootstrap.currentTime);
        }

        private void OnDestroy()
        {
            ShutdownAsync().Forget();
        }

        public void SetConnected(bool isConnected)
        {
            if (_bootstrap == null || _isShuttingDown)
                return;

            _bootstrap.SetConnected(isConnected);
        }

        public void SetNetworkSimulationSettings(NetworkSimulationSettings settings)
        {
            if (_bootstrap == null || _isShuttingDown)
                return;

            _bootstrap.SetNetworkSimulationSettings(settings);
        }

        public void RecreateClient()
        {
            if (_bootstrap != null && _isShuttingDown == false && _isRecreatingClient == false)
                RecreateClientAsync(_lifetime.Token).Forget();
        }

        public void RestartScene()
        {
            if (_isShuttingDown == false)
                RestartSceneAsync().Forget();
        }

        private UniTask ShutdownAsync()
        {
            if (_shutdownCompletion == null)
            {
                _shutdownCompletion = new UniTaskCompletionSource();
                ShutdownCoreAsync().Forget();
            }

            return _shutdownCompletion.Task;
        }

        private async UniTaskVoid ShutdownCoreAsync()
        {
            _isShuttingDown = true;

            try
            {
                DisposePresenters();
                _lifetime?.Cancel();

                if (_startupCompletion != null)
                    await _startupCompletion.Task;

                _lifetime?.Dispose();
                _lifetime = null;

                if (_bootstrap != null)
                {
                    await _bootstrap.ShutdownAsync(CancellationToken.None);
                    _bootstrap = null;
                }

                _isStarted = false;
                _shutdownCompletion.TrySetResult();
            }
            catch (Exception exception)
            {
                _shutdownCompletion.TrySetException(exception);
            }
        }

        private async UniTaskVoid StartGameAsync()
        {
            _lifetime = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy());
            _startupCompletion = new UniTaskCompletionSource();

            try
            {
                _bootstrap = CreateBootstrap();
                await _bootstrap.StartAsync(_lifetime.Token).SuppressCancellationThrow();
                _isStarted = true;

                if (_isShuttingDown == false)
                    CreatePresenters();
            }
            finally
            {
                _startupCompletion.TrySetResult();
            }
        }

        private GameBootstrap CreateBootstrap()
        {
            var settings = new GameBootstrapSettings(
                _gameRules.CreateRuntimeRules(),
                _matchSeed,
                _session.CreateRuntimeSettings(),
                CreateClientNetworkSettings());

            return role == GameProcessRole.Server
                ? new GameBootstrap(settings, GameProcessRole.Server)
                : new GameBootstrap(settings, GameProcessRole.Client, processClientIdentity);
        }

        private ClientNetworkSettings CreateClientNetworkSettings()
        {
            return new ClientNetworkSettings(
                _networkSimulation.CreateRuntimeSettings(),
                _clientRecovery.CreateRuntimeSettings());
        }

        private void CreatePresenters()
        {
            if (_gameView)
            {
                _gamePresenter = role == GameProcessRole.Server
                    ? new ServerGamePresenter(_bootstrap.server, _gameView)
                    : new ClientGamePresenter(_bootstrap.client, _gameView);
                _gamePresenter.Initialize();
            }

            if (role == GameProcessRole.Client)
            {
                _bootstrap.messageLogged += OnMessageLogged;
                _debugPresenter = new DebugPanelPresenter(_debugPanelView, this);
                _debugPresenter.Initialize();
            }
        }

        private void ConfigureUi()
        {
            if (_gameView)
                _gameView.gameObject.SetActive(true);
        }

        private async UniTaskVoid RecreateClientAsync(CancellationToken cancellationToken)
        {
            _isRecreatingClient = true;

            try
            {
                DisposeGamePresenter();
                await _bootstrap.RecreateClientAsync(cancellationToken).SuppressCancellationThrow();

                if (_isShuttingDown == false && _gameView != null)
                {
                    _gamePresenter = new ClientGamePresenter(_bootstrap.client, _gameView);
                    _gamePresenter.Initialize();
                }
            }
            finally
            {
                _isRecreatingClient = false;
            }
        }

        private async UniTaskVoid RestartSceneAsync()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            await ShutdownAsync();
            await SceneManager.LoadSceneAsync(currentScene.buildIndex, LoadSceneMode.Single);
        }

        private void DisposeGamePresenter()
        {
            _gamePresenter?.Dispose();
            _gamePresenter = null;
        }

        private void DisposePresenters()
        {
            DisposeGamePresenter();
            _debugPresenter?.Dispose();
            _debugPresenter = null;

            if (_bootstrap != null)
                _bootstrap.messageLogged -= OnMessageLogged;
        }

        private void OnMessageLogged(TransportLogEntry entry)
        {
            messageLogged?.Invoke(entry);
        }
    }
}
