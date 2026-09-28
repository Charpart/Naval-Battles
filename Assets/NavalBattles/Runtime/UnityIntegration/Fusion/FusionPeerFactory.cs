using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using NavalBattles.Runtime.UnityIntegration.Fusion.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NavalBattles.Runtime.UnityIntegration.Fusion
{
    public sealed class FusionPeerFactory
    {
        private const int PLAYER_COUNT = 2;
        private const int CLIENT_START_RETRY_DELAY_MILLISECONDS = 500;
        private const double CLIENT_START_TIMEOUT_SECONDS = 10.0;
        private const string NETWORK_CONFIG_RESOURCE_PATH = "NetworkProjectConfig";
#if UNITY_EDITOR
        private const string NETWORK_CONFIG_ASSET_PATH =
            "Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion";
#endif

        public UniTask<FusionPeer> StartServerAsync(
            SessionSettings settings,
            CancellationToken cancellationToken)
        {
            return StartPeerAsync(settings, GameMode.Server, cancellationToken);
        }

        public async UniTask<FusionPeer> StartClientAsync(
            SessionSettings settings,
            CancellationToken cancellationToken)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + CLIENT_START_TIMEOUT_SECONDS;

            while (true)
            {
                try
                {
                    return await StartPeerAsync(settings, GameMode.Client, cancellationToken);
                }
                catch (FusionStartException exception) when (
                    exception.shutdownReason == ShutdownReason.GameNotFound
                    && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    await UniTask.Delay(
                        CLIENT_START_RETRY_DELAY_MILLISECONDS,
                        cancellationToken: cancellationToken);
                }
            }
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
                Config = LoadNetworkConfig(),
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

        private static NetworkProjectConfig LoadNetworkConfig()
        {
            NetworkProjectConfigAsset configAsset =
                Resources.Load<NetworkProjectConfigAsset>(NETWORK_CONFIG_RESOURCE_PATH);
#if UNITY_EDITOR
            configAsset ??= AssetDatabase.LoadAssetAtPath<NetworkProjectConfigAsset>(
                NETWORK_CONFIG_ASSET_PATH);
#endif

            if (configAsset == null)
            {
                throw new InvalidOperationException(
                    $"Fusion network config was not found in Resources/{NETWORK_CONFIG_RESOURCE_PATH}.");
            }

            return configAsset.Config;
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
                throw new FusionStartException(gameMode, result.ShutdownReason);
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

        private sealed class FusionStartException : InvalidOperationException
        {
            public ShutdownReason shutdownReason { get; }

            public FusionStartException(GameMode gameMode, ShutdownReason shutdownReason)
                : base($"Fusion {gameMode} failed: {shutdownReason}.")
            {
                this.shutdownReason = shutdownReason;
            }
        }
    }
}
