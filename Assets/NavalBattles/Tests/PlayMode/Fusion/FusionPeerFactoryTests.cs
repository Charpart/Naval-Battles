using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NavalBattles.Runtime.Transport;
using NavalBattles.Runtime.UnityIntegration.Fusion;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace NavalBattles.Tests.PlayMode.Fusion
{
    [TestFixture]
    public sealed class FusionPeerFactoryTests
    {
        private const int NETWORK_SCENE_BUILD_INDEX = 1;

        private FusionPeer _serverPeer;
        private FusionPeer _clientPeer;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            return ShutdownAsync().ToCoroutine();
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator StartPeers_WhenMessagesAreSent_DeliversInBothDirections()
        {
            return VerifyMessageDeliveryAsync().ToCoroutine();
        }

        private async UniTask VerifyMessageDeliveryAsync()
        {
            // Arrange
            var settings = new SessionSettings(
                $"naval-transport-{Guid.NewGuid():N}",
                NETWORK_SCENE_BUILD_INDEX);
            var factory = new FusionPeerFactory();
            _serverPeer = await factory.StartServerAsync(settings, CancellationToken.None);
            _clientPeer = await factory.StartClientAsync(settings, CancellationToken.None);
            await UniTask.WaitUntil(() => _clientPeer.runner.IsConnectedToServer);
            var clientPayload = new byte[] { 4, 8, 15 };
            byte[] serverReceived = null;
            byte[] clientReceived = null;
            var clientConnection = default(TransportConnectionId);
            _serverPeer.transport.OnReceived += (source, payload) =>
            {
                clientConnection = source;
                serverReceived = payload.ToArray();
            };
            _clientPeer.transport.OnReceived += (_, payload) => clientReceived = payload.ToArray();

            // Act
            _clientPeer.transport.Send(new TransportConnectionId(0), clientPayload);
            await UniTask.WaitUntil(() => serverReceived != null);
            _serverPeer.transport.Send(clientConnection, new byte[] { 16, 23, 42 });
            await UniTask.WaitUntil(() => clientReceived != null);

            // Assert
            Assert.That(clientConnection.value, Is.GreaterThan(0));
            Assert.That(serverReceived, Is.EqualTo(new byte[] { 4, 8, 15 }));
            Assert.That(clientReceived, Is.EqualTo(new byte[] { 16, 23, 42 }));
        }

        private async UniTask ShutdownAsync()
        {
            if (_clientPeer != null)
            {
                await _clientPeer.ShutdownAsync(CancellationToken.None);
                _clientPeer = null;
            }

            if (_serverPeer != null)
            {
                await _serverPeer.ShutdownAsync(CancellationToken.None);
                _serverPeer = null;
            }
        }
    }
}
