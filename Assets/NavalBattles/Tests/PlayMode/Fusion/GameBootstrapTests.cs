using System;
using System.Collections;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using NavalBattles.Runtime.Client;
using NavalBattles.Runtime.Client.Connection;
using NavalBattles.Runtime.Client.Identity;
using NavalBattles.Runtime.Domain.Configuration;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.UnityIntegration.Bootstrap;
using NavalBattles.Runtime.UnityIntegration.Configuration;
using NavalBattles.Runtime.UnityIntegration.Fusion;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace NavalBattles.Tests.PlayMode.Fusion
{
    [TestFixture]
    public sealed class GameBootstrapTests
    {
        private const int NETWORK_SCENE_BUILD_INDEX = 1;

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
        public IEnumerator StartAsync_WhenTwoPlayersFire_SynchronizesPersonalSnapshots()
        {
            return VerifyMatchFlowAsync().ToCoroutine();
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator RecreateClientAsync_WhenConnectionChanges_RestoresSession()
        {
            return VerifyClientRecreationAsync().ToCoroutine();
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator SetConnected_WhenLinkReturns_ResumesSession()
        {
            return VerifySilentDisconnectRecoveryAsync().ToCoroutine();
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator StartAsync_WhenMatchCreationFails_ShutsDownServerRunner()
        {
            return VerifyFailedMatchCleanupAsync().ToCoroutine();
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator ShutdownAsync_DuringClientRecreation_LeavesNoReplacementRunner()
        {
            return VerifyShutdownDuringRecreationAsync().ToCoroutine();
        }

        private async UniTask VerifyMatchFlowAsync()
        {
            // Arrange
            await StartCompositionAsync();

            // Act
            await WaitUntilAsync(AreClientsConnected);
            GameClient firstShooter = GetActiveClient();
            ulong initialRevision = firstShooter.snapshot.revision;
            Assert.That(firstShooter.TryFire(0), Is.True);
            await WaitForRevisionAsync(initialRevision + 1);
            GameClient secondShooter = GetActiveClient();
            Assert.That(secondShooter, Is.Not.SameAs(firstShooter));
            Assert.That(secondShooter.TryFire(0), Is.True);
            await WaitForRevisionAsync(initialRevision + 2);

            // Assert
            Assert.That(_server.server, Is.Not.Null);
            Assert.That(_server.client, Is.Null);
            Assert.That(_firstClient.client.player, Is.Not.EqualTo(_secondClient.client.player));
            Assert.That(_firstClient.client.snapshot.ownShipIndices, Has.Some.GreaterThanOrEqualTo(0));
            Assert.That(_secondClient.client.snapshot.ownShipIndices, Has.Some.GreaterThanOrEqualTo(0));
            Assert.That(_firstClient.client.snapshot.opponentShots[0], Is.Not.EqualTo(NetworkShotState.None));
            Assert.That(_secondClient.client.snapshot.opponentShots[0], Is.Not.EqualTo(NetworkShotState.None));
            Assert.That(_server.serverRevision, Is.EqualTo(initialRevision + 2));
            Assert.That(_firstClient.connectedPlayerCount, Is.Zero);
            Assert.That(_secondClient.connectedPlayerCount, Is.Zero);
        }

        private async UniTask VerifyClientRecreationAsync()
        {
            // Arrange
            await StartCompositionAsync();
            await WaitUntilAsync(AreClientsConnected);
            GameClient previousClient = _firstClient.client;
            NetworkPlayerSlot previousPlayer = previousClient.player;
            ulong expectedRevision = _server.serverRevision;

            // Act
            await RecreateClientWithTicksAsync();
            await WaitUntilAsync(AreClientsConnected);

            // Assert
            Assert.That(_firstClient.client, Is.Not.SameAs(previousClient));
            Assert.That(_firstClient.client.player, Is.EqualTo(previousPlayer));
            Assert.That(_firstClient.client.snapshot.revision, Is.EqualTo(expectedRevision));
            Assert.That(_server.connectedPlayerCount, Is.EqualTo(2));
        }

        private async UniTask VerifySilentDisconnectRecoveryAsync()
        {
            // Arrange
            await StartCompositionAsync();
            await WaitUntilAsync(AreClientsConnected);
            NetworkPlayerSlot expectedPlayer = _firstClient.client.player;
            ulong expectedRevision = _server.serverRevision;

            // Act
            _firstClient.SetConnected(false);
            await WaitUntilAsync(
                () => _firstClient.client.connectionState == ClientConnectionState.Disconnected);
            _firstClient.SetConnected(true);
            await WaitUntilAsync(AreClientsConnected);

            // Assert
            Assert.That(_firstClient.client.player, Is.EqualTo(expectedPlayer));
            Assert.That(_firstClient.client.snapshot.revision, Is.EqualTo(expectedRevision));
            Assert.That(_server.connectedPlayerCount, Is.EqualTo(2));
        }

        private async UniTask VerifyFailedMatchCleanupAsync()
        {
            // Arrange
            int[] shipLengths = Enumerable.Repeat(1, 128).ToArray();
            bool wereRulesCreated = GameRules.TryCreate(
                12,
                12,
                shipLengths,
                15.0,
                out GameRules rules,
                out GameRulesValidationError error);
            Assert.That(wereRulesCreated, Is.True, error.ToString());
            var settings = new GameBootstrapSettings(
                rules,
                42,
                new SessionSettings($"naval-invalid-{Guid.NewGuid():N}", NETWORK_SCENE_BUILD_INDEX),
                ClientNetworkSettings.Default);
            _server = new GameBootstrap(settings, GameProcessRole.Server);

            // Act
            try
            {
                await _server.StartAsync(CancellationToken.None);
                Assert.Fail("Invalid match configuration must fail startup.");
            }
            catch (ArgumentException)
            {
            }

            await UniTask.Yield();

            // Assert
            Assert.That(
                NetworkRunner.Instances.Any(runner => runner.name == "Naval Battles Server"),
                Is.False);
        }

        private async UniTask VerifyShutdownDuringRecreationAsync()
        {
            // Arrange
            await StartCompositionAsync();
            await WaitUntilAsync(AreClientsConnected);
            int expectedClientRunnerCount = NetworkRunner.Instances.Count(
                runner => runner.name == "Naval Battles Client") - 1;

            // Act
            UniTask recreation = _firstClient.RecreateClientAsync(CancellationToken.None);
            await WaitUntilAsync(() => _firstClient.client == null);
            await _firstClient.ShutdownAsync(CancellationToken.None);
            await recreation;
            await WaitUntilAsync(() =>
                NetworkRunner.Instances.Count(
                    runner => runner.name == "Naval Battles Client")
                == expectedClientRunnerCount);

            // Assert
            Assert.That(_firstClient.client, Is.Null);
            Assert.That(
                NetworkRunner.Instances.Count(runner => runner.name == "Naval Battles Client"),
                Is.EqualTo(expectedClientRunnerCount));
        }

        private async UniTask StartCompositionAsync()
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
            var session = new SessionSettings(
                $"naval-recovery-{Guid.NewGuid():N}",
                NETWORK_SCENE_BUILD_INDEX);
            var settings = new GameBootstrapSettings(
                rules,
                42,
                session,
                ClientNetworkSettings.Default);
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
        }

        private async UniTask WaitUntilAsync(Func<bool> predicate)
        {
            double deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 20.0;

            while (predicate() == false && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline)
            {
                TickComposition();
                await UniTask.Yield();
            }

            Assert.That(predicate(), Is.True, "Condition was not reached before timeout.");
        }

        private async UniTask RecreateClientWithTicksAsync()
        {
            UniTask recreation = _firstClient.RecreateClientAsync(CancellationToken.None);

            while (recreation.Status == UniTaskStatus.Pending)
            {
                TickComposition();
                await UniTask.Yield();
            }

            await recreation;
        }

        private bool AreClientsConnected()
        {
            return _server.connectedPlayerCount == 2
                && _firstClient.client.connectionState == ClientConnectionState.Connected
                && _secondClient.client.connectionState == ClientConnectionState.Connected;
        }

        private GameClient GetActiveClient()
        {
            return _firstClient.client.snapshot.activePlayer == _firstClient.client.player
                ? _firstClient.client
                : _secondClient.client;
        }

        private bool AreClientsAtRevision(ulong revision)
        {
            return _firstClient.client.snapshot.revision == revision
                && _secondClient.client.snapshot.revision == revision;
        }

        private async UniTask WaitForRevisionAsync(ulong revision)
        {
            await WaitUntilAsync(() => AreClientsAtRevision(revision));
        }

        private void TickComposition()
        {
            _server?.Tick();
            _firstClient?.Tick();
            _secondClient?.Tick();
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
