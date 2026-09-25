using System;
using NavalBattles.Runtime.Transport;
using NavalBattles.Tests.EditMode.Fakes;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Transport
{
    [TestFixture]
    public sealed class MessageTransportTests
    {
        [Test]
        public void Send_WhenSourceBufferChanges_KeepsSentPayload()
        {
            // Arrange
            var transport = new FakeMessageTransport();
            var target = new TransportConnectionId(7);
            byte[] source = { 1, 2, 3 };

            // Act
            transport.Send(target, source);
            source[0] = 99;

            // Assert
            Assert.That(transport.sentMessages[0].target, Is.EqualTo(target));
            Assert.That(transport.sentMessages[0].payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void Deliver_WhenSourceBufferChanges_KeepsReceivedPayload()
        {
            // Arrange
            var transport = new FakeMessageTransport();
            var sourceConnection = new TransportConnectionId(9);
            byte[] source = { 4, 5, 6 };
            ReadOnlyMemory<byte> received = default;
            transport.OnReceived += (_, payload) => received = payload;

            // Act
            transport.Deliver(sourceConnection, source);
            source[0] = 99;

            // Assert
            Assert.That(received.ToArray(), Is.EqualTo(new byte[] { 4, 5, 6 }));
        }

        [Test]
        public void ConnectionId_WithSameValue_HasValueSemantics()
        {
            // Arrange
            var first = new TransportConnectionId(12);
            var second = new TransportConnectionId(12);

            // Assert
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first == second, Is.True);
            Assert.That(first != second, Is.False);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }
    }
}
