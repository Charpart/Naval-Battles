using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NavalBattles.Runtime.Client.Connection;
using NavalBattles.Runtime.Client.Identity;
using NavalBattles.Runtime.Domain.Configuration;
using NavalBattles.Runtime.UnityIntegration.Bootstrap;
using NavalBattles.Runtime.UnityIntegration.Configuration;
using NavalBattles.Runtime.UnityIntegration.Fusion;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace NavalBattles.Tests.PlayMode.Fusion
{
    [TestFixture]
    public sealed class MultiplayerPlayModeBootstrapTests
    {
        private const int NetworkSceneBuildIndex = 1;

        private GameBootstrap _server;
        private GameBootstrap _firstClient;
        private GameBootstrap _secondClient;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            return ShutdownAsync().ToCoroutine();
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator StartAsync_InSeparateRoles_ConnectsTwoClientsToOneServer()
        {
            return VerifySeparateProcessesAsync().ToCoroutine();
        }

        [Test]
        public void ProcessClientIdentity_WhenReadAfterSceneReload_PreservesCommandSequence()
        {
            ClientIdentityStore beforeReload = GameSceneController.processClientIdentity;
            ulong beforeReloadCommandId = beforeReload.ReserveCommandId();

            ClientIdentityStore afterReload = GameSceneController.processClientIdentity;
            ulong afterReloadCommandId = afterReload.ReserveCommandId();

            Assert.That(afterReload, Is.SameAs(beforeReload));
            Assert.That(afterReload.clientId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(afterReloadCommandId, Is.EqualTo(beforeReloadCommandId + 1));
        }

        private async UniTask VerifySeparateProcessesAsync()
        {
            GameBootstrapSettings settings = CreateSettings();
            _server = new GameBootstrap(settings, GameProcessRole.Server);
            _firstClient = new GameBootstrap(
                settings,
                GameProcessRole.Client,
                new ClientIdentityStore(Guid.NewGuid()));
            _secondClient = new GameBootstrap(
                settings,
                GameProcessRole.Client,
                new ClientIdentityStore(Guid.NewGuid()));

            await _server.StartAsync(CancellationToken.None);
            await _firstClient.StartAsync(CancellationToken.None);
            await _secondClient.StartAsync(CancellationToken.None);
            await WaitUntilAsync(AreAllConnected);

            Assert.That(_server.connectedPlayerCount, Is.EqualTo(2));
            Assert.That(_server.client, Is.Null);
            Assert.That(_firstClient.client, Is.Not.Null);
            Assert.That(_secondClient.client, Is.Not.Null);
            Assert.That(_firstClient.connectedPlayerCount, Is.Zero);
            Assert.That(_secondClient.connectedPlayerCount, Is.Zero);
            Assert.That(
                _firstClient.client.player,
                Is.Not.EqualTo(_secondClient.client.player));
        }

        private bool AreAllConnected()
        {
            return _server.connectedPlayerCount == 2
                && _firstClient.client.connectionState == ClientConnectionState.Connected
                && _secondClient.client.connectionState == ClientConnectionState.Connected;
        }

        private async UniTask WaitUntilAsync(Func<bool> predicate)
        {
            double deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 20.0;

            while (predicate() == false && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline)
            {
                _server.Tick();
                _firstClient.Tick();
                _secondClient.Tick();
                await UniTask.Yield();
            }

            Assert.That(predicate(), Is.True, "Clients did not connect before timeout.");
        }

        private static GameBootstrapSettings CreateSettings()
        {
            int[] shipLengths = { 3, 2, 2, 1 };
            bool wereRulesCreated = GameRules.TryCreate(
                6,
                6,
                shipLengths,
                15.0,
                out GameRules rules,
                out GameRulesValidationError error);
            Assert.That(wereRulesCreated, Is.True, error.ToString());

            return new GameBootstrapSettings(
                rules,
                42,
                new SessionSettings(
                    $"naval-mppm-{Guid.NewGuid():N}",
                    NetworkSceneBuildIndex),
                ClientNetworkSettings.Default);
        }

        private async UniTask ShutdownAsync()
        {
            if (_secondClient != null)
            {
                await _secondClient.ShutdownAsync(CancellationToken.None);
            }

            if (_firstClient != null)
            {
                await _firstClient.ShutdownAsync(CancellationToken.None);
            }

            if (_server != null)
            {
                await _server.ShutdownAsync(CancellationToken.None);
            }
        }
    }
}
