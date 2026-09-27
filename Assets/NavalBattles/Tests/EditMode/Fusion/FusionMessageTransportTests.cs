using System;
using System.Threading;
using Fusion;
using Fusion.Sockets;
using NavalBattles.Runtime.Transport;
using NavalBattles.Runtime.UnityIntegration.Fusion.Transport;
using NUnit.Framework;
using UnityEngine;

namespace NavalBattles.Tests.EditMode.Fusion
{
    [TestFixture]
    public sealed class FusionMessageTransportTests
    {
        private GameObject _runnerObject;
        private NetworkRunner _runner;
        private CancellationTokenSource _lifetime;
        private FusionMessageTransport _transport;

        [SetUp]
        public void SetUp()
        {
            _runnerObject = new GameObject("Fusion Message Transport Test");
            _runner = _runnerObject.AddComponent<NetworkRunner>();
            _lifetime = new CancellationTokenSource();
        }

        [TearDown]
        public void TearDown()
        {
            _transport?.Dispose();
            _lifetime.Dispose();
            UnityEngine.Object.DestroyImmediate(_runnerObject);
        }

        [Test]
        public void OnReliableDataReceived_OnServer_MapsPlayerAndCopiesPayload()
        {
            // Arrange
            _transport = FusionMessageTransport.CreateServer(_runner, _lifetime.Token);
            var source = default(TransportConnectionId);
            ReadOnlyMemory<byte> received = default;
            _transport.OnReceived += (connectionId, payload) =>
            {
                source = connectionId;
                received = payload;
            };
            byte[] callbackBuffer = { 4, 8, 15 };

            // Act
            _transport.OnReliableDataReceived(
                _runner,
                PlayerRef.FromRaw(7),
                default(ReliableKey),
                callbackBuffer);
            callbackBuffer[0] = 99;

            // Assert
            Assert.That(source, Is.EqualTo(new TransportConnectionId(7)));
            Assert.That(received.Span[0], Is.EqualTo(4));
        }

        [Test]
        public void OnReliableDataReceived_OnClient_MapsSourceToServerConnection()
        {
            // Arrange
            _transport = FusionMessageTransport.CreateClient(_runner, _lifetime.Token);
            var source = new TransportConnectionId(-1);
            _transport.OnReceived += (connectionId, _) => source = connectionId;

            // Act
            _transport.OnReliableDataReceived(
                _runner,
                PlayerRef.None,
                default(ReliableKey),
                new byte[] { 1 });

            // Assert
            Assert.That(source, Is.EqualTo(new TransportConnectionId(0)));
        }

        [Test]
        public void OnReliableDataReceived_FromDifferentRunner_IsIgnored()
        {
            // Arrange
            _transport = FusionMessageTransport.CreateServer(_runner, _lifetime.Token);
            bool wasReceived = false;
            _transport.OnReceived += (_, _) => wasReceived = true;
            var otherObject = new GameObject("Other Fusion Runner");
            NetworkRunner otherRunner = otherObject.AddComponent<NetworkRunner>();

            try
            {
                // Act
                _transport.OnReliableDataReceived(
                    otherRunner,
                    PlayerRef.FromRaw(2),
                    default(ReliableKey),
                    new byte[] { 1 });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(otherObject);
            }

            // Assert
            Assert.That(wasReceived, Is.False);
        }

        [Test]
        public void OnReliableDataReceived_AfterLifetimeCancellation_IsIgnored()
        {
            // Arrange
            _transport = FusionMessageTransport.CreateServer(_runner, _lifetime.Token);
            bool wasReceived = false;
            _transport.OnReceived += (_, _) => wasReceived = true;
            _lifetime.Cancel();

            // Act
            _transport.OnReliableDataReceived(
                _runner,
                PlayerRef.FromRaw(2),
                default(ReliableKey),
                new byte[] { 1 });

            // Assert
            Assert.That(wasReceived, Is.False);
        }
    }
}
