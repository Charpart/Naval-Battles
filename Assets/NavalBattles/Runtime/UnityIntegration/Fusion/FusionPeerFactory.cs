using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using NavalBattles.Runtime.UnityIntegration.Fusion.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NavalBattles.Runtime.UnityIntegration.Fusion
{
    public sealed class FusionPeerFactory
    {
        private const int PLAYER_COUNT = 2;

        public UniTask<FusionPeer> StartServerAsync(
            SessionSettings settings,
            CancellationToken cancellationToken)
        {
            return StartPeerAsync(settings, GameMode.Server, cancellationToken);
        }

        public UniTask<FusionPeer> StartClientAsync(
            SessionSettings settings,
            CancellationToken cancellationToken)
        {
            return StartPeerAsync(settings, GameMode.Client, cancellationToken);
        }

        private static async UniTask<FusionPeer> StartPeerAsync(
            SessionSettings settings,
            GameMode gameMode,
            CancellationToken cancellationToken)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            NetworkRunner runner = CreateRunner($"Naval Battles {gameMode}");
            FusionMessageTransport transport = CreateTransport(runner, gameMode, cancellationToken);

            try
            {
                StartGameResult result = await runner.StartGame(CreateStartArguments(
                    runner,
                    settings,
                    gameMode,
                    cancellationToken));
                EnsureStarted(result, gameMode);

                return new FusionPeer(runner, transport);
            }
            catch
            {
                await CleanupFailedStartAsync(runner, transport);
                throw;
            }
        }

        private static StartGameArgs CreateStartArguments(
            NetworkRunner runner,
            SessionSettings settings,
            GameMode gameMode,
            CancellationToken cancellationToken)
        {
            return new StartGameArgs
            {
                GameMode = gameMode,
                SessionName = settings.sessionName,
                PlayerCount = PLAYER_COUNT,
                Scene = CreateSceneInfo(settings.sceneBuildIndex),
                SceneManager = runner.GetComponent<NetworkSceneManagerDefault>(),
                ObjectProvider = runner.GetComponent<NetworkObjectProviderDefault>(),
                EnableClientSessionCreation = gameMode == GameMode.Server,
                IsVisible = false,
                StartGameCancellationToken = cancellationToken
            };
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

        private static FusionMessageTransport CreateTransport(
            NetworkRunner runner,
            GameMode gameMode,
            CancellationToken cancellationToken)
        {
            return gameMode == GameMode.Server
                ? FusionMessageTransport.CreateServer(runner, cancellationToken)
                : FusionMessageTransport.CreateClient(runner, cancellationToken);
        }

        private static NetworkSceneInfo CreateSceneInfo(int sceneBuildIndex)
        {
            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(sceneBuildIndex), LoadSceneMode.Additive);

            return sceneInfo;
        }

        private static void EnsureStarted(StartGameResult result, GameMode gameMode)
        {
            if (result.Ok == false)
                throw new InvalidOperationException($"Fusion {gameMode} failed: {result.ShutdownReason}.");
        }

        private static async UniTask CleanupFailedStartAsync(
            NetworkRunner runner,
            FusionMessageTransport transport)
        {
            transport.Dispose();

            if (runner && runner.IsShutdown == false)
                await runner.Shutdown();

            if (runner)
                UnityEngine.Object.Destroy(runner.gameObject);
        }
    }
}
