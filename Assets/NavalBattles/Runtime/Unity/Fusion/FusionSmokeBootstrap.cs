using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NavalBattles.Runtime.Unity.Fusion
{
    public sealed class FusionSmokeBootstrap
    {
        private const int PLAYER_COUNT = 2;

        private readonly string _sessionName;
        private readonly int _sceneBuildIndex;
        private readonly List<NetworkRunner> _clientRunners = new List<NetworkRunner>(PLAYER_COUNT);
        private NetworkRunner _serverRunner;

        public bool isServerRunning => _serverRunner != null && _serverRunner.IsRunning;

        public int connectedClientCount
        {
            get
            {
                int count = 0;

                for (int index = 0; index < _clientRunners.Count; index++)
                {
                    if (_clientRunners[index] != null && _clientRunners[index].IsConnectedToServer)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public FusionSmokeBootstrap(string sessionName, int sceneBuildIndex)
        {
            if (string.IsNullOrWhiteSpace(sessionName))
            {
                throw new ArgumentException("Session name must not be empty.", nameof(sessionName));
            }

            _sessionName = sessionName;
            _sceneBuildIndex = sceneBuildIndex;
        }

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            if (_serverRunner != null)
            {
                throw new InvalidOperationException("Fusion peers have already been started.");
            }

            _serverRunner = CreateRunner("Fusion Smoke Server");
            await StartPeerAsync(_serverRunner, GameMode.Server, true, cancellationToken);

            for (int clientIndex = 0; clientIndex < PLAYER_COUNT; clientIndex++)
            {
                NetworkRunner clientRunner = CreateRunner($"Fusion Smoke Client {clientIndex + 1}");
                _clientRunners.Add(clientRunner);
                await StartPeerAsync(clientRunner, GameMode.Client, false, cancellationToken);
            }

            await UniTask.WaitUntil(ArePeersReady, cancellationToken: cancellationToken);
        }

        public async UniTask ShutdownAsync(CancellationToken cancellationToken)
        {
            for (int index = _clientRunners.Count - 1; index >= 0; index--)
            {
                await ShutdownRunnerAsync(_clientRunners[index], cancellationToken);
            }

            _clientRunners.Clear();
            await ShutdownRunnerAsync(_serverRunner, cancellationToken);
            _serverRunner = null;
        }

        private async UniTask StartPeerAsync(
            NetworkRunner runner,
            GameMode gameMode,
            bool canCreateSession,
            CancellationToken cancellationToken)
        {
            NetworkSceneInfo sceneInfo = CreateSceneInfo();
            var arguments = new StartGameArgs
            {
                GameMode = gameMode,
                SessionName = _sessionName,
                PlayerCount = PLAYER_COUNT,
                Scene = sceneInfo,
                SceneManager = runner.GetComponent<NetworkSceneManagerDefault>(),
                ObjectProvider = runner.GetComponent<NetworkObjectProviderDefault>(),
                EnableClientSessionCreation = canCreateSession,
                IsVisible = false,
                StartGameCancellationToken = cancellationToken
            };
            StartGameResult result = await runner.StartGame(arguments);

            if (result.Ok == false)
            {
                throw new InvalidOperationException($"Fusion {gameMode} failed: {result.ShutdownReason}.");
            }
        }

        private NetworkSceneInfo CreateSceneInfo()
        {
            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(_sceneBuildIndex), LoadSceneMode.Additive);

            return sceneInfo;
        }

        private bool ArePeersReady()
        {
            if (_serverRunner.IsSceneManagerBusy || connectedClientCount != PLAYER_COUNT)
            {
                return false;
            }

            for (int index = 0; index < _clientRunners.Count; index++)
            {
                if (_clientRunners[index].IsSceneManagerBusy)
                {
                    return false;
                }
            }

            return true;
        }

        private static NetworkRunner CreateRunner(string name)
        {
            var runnerObject = new GameObject(name);
            UnityEngine.Object.DontDestroyOnLoad(runnerObject);
            var runner = runnerObject.AddComponent<NetworkRunner>();
            runner.ProvideInput = false;
            runnerObject.AddComponent<NetworkSceneManagerDefault>();
            runnerObject.AddComponent<NetworkObjectProviderDefault>();

            return runner;
        }

        private static async UniTask ShutdownRunnerAsync(
            NetworkRunner runner,
            CancellationToken cancellationToken)
        {
            if (runner == null)
            {
                return;
            }

            if (runner.IsShutdown == false)
            {
                await runner.Shutdown();
            }

            cancellationToken.ThrowIfCancellationRequested();
            UnityEngine.Object.Destroy(runner.gameObject);
        }
    }
}
