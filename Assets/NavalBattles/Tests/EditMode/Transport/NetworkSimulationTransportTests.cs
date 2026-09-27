using System.Collections.Generic;
using NavalBattles.Runtime.Transport;
using NavalBattles.Runtime.Transport.Simulation;
using NavalBattles.Tests.EditMode.Fakes;
using NUnit.Framework;
using NavalBattles.Runtime.Transport.Diagnostics;

namespace NavalBattles.Tests.EditMode.Transport
{
    [TestFixture]
    public sealed class NetworkSimulationTransportTests
    {
        private static readonly TransportConnectionId Connection = new TransportConnectionId(7);

        private FakeMessageTransport _inner;
        private List<TransportLogEntry> _log;

        [SetUp]
        public void SetUp()
        {
            _inner = new FakeMessageTransport();
            _log = new List<TransportLogEntry>();
        }

        [Test]
        public void Send_WithDelay_DeliversOnlyAfterScheduledTime()
        {
            // Arrange
            using NetworkSimulationTransport transport = CreateTransport(
                new NetworkSimulationSettings(2.0, 0.0, 0.0, 0.0));

            // Act
            transport.Send(Connection, new byte[] { 4 });
            transport.Tick(1.99);
            int countBeforeDeadline = _inner.sentMessages.Count;
            transport.Tick(2.0);

            // Assert
            Assert.That(countBeforeDeadline, Is.EqualTo(0));
            Assert.That(_inner.sentMessages, Has.Count.EqualTo(1));
            Assert.That(_inner.sentMessages[0].payload, Is.EqualTo(new byte[] { 4 }));
        }

        [Test]
        public void Send_WithFullLoss_DropsMessageAndRecordsIt()
        {
            // Arrange
            using NetworkSimulationTransport transport = CreateTransport(
                new NetworkSimulationSettings(0.0, 0.0, 1.0, 0.0));

            // Act
            transport.Send(Connection, new byte[] { 4 });
            transport.Tick(0.0);

            // Assert
            Assert.That(_inner.sentMessages, Is.Empty);
            Assert.That(_log, Has.Some.Matches<TransportLogEntry>(entry =>
                entry.status == TransportMessageStatus.Dropped));
        }

        [Test]
        public void Send_WithFullDuplication_DeliversTwoIndependentCopies()
        {
            // Arrange
            using NetworkSimulationTransport transport = CreateTransport(
                new NetworkSimulationSettings(0.0, 0.0, 0.0, 1.0));
            byte[] payload = { 4 };

            // Act
            transport.Send(Connection, payload);
            payload[0] = 99;
            transport.Tick(0.0);

            // Assert
            Assert.That(_inner.sentMessages, Has.Count.EqualTo(2));
            Assert.That(_inner.sentMessages[0].payload[0], Is.EqualTo(4));
            Assert.That(_inner.sentMessages[1].payload[0], Is.EqualTo(4));
        }

        [Test]
        public void Receive_WithDelay_PreservesDeliveryOrder()
        {
            // Arrange
            using NetworkSimulationTransport transport = CreateTransport(
                new NetworkSimulationSettings(2.0, 0.0, 0.0, 0.0));
            var received = new List<byte>();
            transport.OnReceived += (_, payload) => received.Add(payload.Span[0]);

            // Act
            _inner.Deliver(Connection, new byte[] { 1 });
            transport.Tick(1.0);
            _inner.Deliver(Connection, new byte[] { 2 });
            transport.Tick(3.0);

            // Assert
            Assert.That(received, Is.EqualTo(new byte[] { 1, 2 }));
        }

        [Test]
        public void SetConnected_WhenDisconnected_DropsTrafficWithoutNotification()
        {
            // Arrange
            using NetworkSimulationTransport transport = CreateTransport(NetworkSimulationSettings.None);
            bool wasReceived = false;
            transport.OnReceived += (_, _) => wasReceived = true;
            transport.SetConnected(false);

            // Act
            transport.Send(Connection, new byte[] { 1 });
            _inner.Deliver(Connection, new byte[] { 2 });
            transport.Tick(0.0);

            // Assert
            Assert.That(_inner.sentMessages, Is.Empty);
            Assert.That(wasReceived, Is.False);
            Assert.That(_log.FindAll(entry => entry.status == TransportMessageStatus.Dropped), Has.Count.EqualTo(2));
        }

        private NetworkSimulationTransport CreateTransport(NetworkSimulationSettings settings)
        {
            var transport = new NetworkSimulationTransport(_inner, settings, 42);
            transport.OnMessageLogged += _log.Add;

            return transport;
        }
    }
}
