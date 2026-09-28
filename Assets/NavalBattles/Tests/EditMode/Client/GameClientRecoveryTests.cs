using System;
using System.Linq;
using NavalBattles.Runtime.Client;
using NavalBattles.Runtime.Client.Connection;
using NavalBattles.Runtime.Client.Identity;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Serialization;
using NavalBattles.Runtime.Protocol.Snapshots;
using NavalBattles.Runtime.Transport;
using NavalBattles.Tests.EditMode.Fakes;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Client
{
    [TestFixture]
    public sealed class GameClientRecoveryTests
    {
        private static readonly TransportConnectionId ServerConnection = new TransportConnectionId(0);
        private static readonly ClientRecoverySettings RecoverySettings =
            new ClientRecoverySettings(2.0, 5.0, 1.0);

        private FakeMessageTransport _transport;
        private IProtocolSerializer _serializer;
        private GameClient _client;

        [SetUp]
        public void SetUp()
        {
            _transport = new FakeMessageTransport();
            _serializer = new BinaryProtocolSerializer();
            _client = new GameClient(
                _transport,
                new ClientIdentityStore(Guid.NewGuid()),
                ServerConnection,
                _serializer,
                RecoverySettings);
            _client.Tick(100.0);
            _client.Connect();
            _client.Receive(_serializer.Serialize(CreateSessionAccepted(CreateSnapshot(1, 10))));
            _transport.Clear();
        }

        [Test]
        public void Tick_WhenHeartbeatIsDue_SendsHeartbeatWithoutReservingCommandId()
        {
            // Act
            _client.Tick(102.0);
            bool wasShotAccepted = _client.TryFire(3);

            // Assert
            ClientMessage[] messages = _transport.sentMessages
                .Select(message => DecodeClient(message.payload))
                .ToArray();
            ClientMessage heartbeat = messages.Single(message =>
                message.type == MessageType.HeartbeatRequest);
            ClientMessage fire = messages.Single(message => message.type == MessageType.FireRequest);
            Assert.That(heartbeat.type, Is.EqualTo(MessageType.HeartbeatRequest));
            Assert.That(wasShotAccepted, Is.True);
            Assert.That(fire.commandId, Is.EqualTo(1));
        }

        [Test]
        public void Tick_WhenServerIsSilent_MarksClientDisconnected()
        {
            // Act
            _client.Tick(105.0);

            // Assert
            Assert.That(_client.connectionState, Is.EqualTo(ClientConnectionState.Disconnected));
        }

        [Test]
        public void Tick_WhenFireResponseIsLost_RetriesSameCommand()
        {
            // Arrange
            _client.TryFire(3);
            ClientMessage original = DecodeClient(_transport.sentMessages[0].payload);

            // Act
            _client.Tick(101.0);

            // Assert
            ClientMessage retry = DecodeClient(_transport.sentMessages[1].payload);
            Assert.That(retry.commandId, Is.EqualTo(original.commandId));
            Assert.That(retry.messageId, Is.EqualTo(original.messageId));
        }

        [Test]
        public void Receive_WhenResumeDoesNotContainPendingCommand_RetriesIt()
        {
            // Arrange
            _client.TryFire(3);
            ClientMessage original = DecodeClient(_transport.sentMessages[0].payload);
            DisconnectAndStartResume();

            // Act
            _client.Receive(_serializer.Serialize(CreateSessionAccepted(CreateSnapshot(2, 11))));

            // Assert
            ClientMessage retry = DecodeClient(_transport.sentMessages[0].payload);
            Assert.That(retry.commandId, Is.EqualTo(original.commandId));
            Assert.That(_client.hasPendingCommand, Is.True);
        }

        [Test]
        public void Receive_WhenResumeContainsPendingCommand_ClearsItWithoutRetry()
        {
            // Arrange
            _client.TryFire(3);
            DisconnectAndStartResume();

            // Act
            _client.Receive(_serializer.Serialize(CreateSessionAccepted(CreateSnapshot(2, 11, 1))));

            // Assert
            Assert.That(_client.hasPendingCommand, Is.False);
            Assert.That(_transport.sentMessages, Is.Empty);
        }

        private void DisconnectAndStartResume()
        {
            _client.Tick(105.0);
            _client.Connect();
            _transport.Clear();
        }

        private static ServerMessage CreateSessionAccepted(PlayerSnapshot snapshot)
        {
            int[] shipLengths = { 3, 2, 2, 1 };
            var definition = new GameDefinition(6, 6, shipLengths, 15.0);

            return ServerMessage.CreateSessionAccepted(
                1,
                NetworkPlayerSlot.First,
                definition,
                snapshot,
                100.0);
        }

        private static PlayerSnapshot CreateSnapshot(
            ulong revision,
            ulong turnId,
            ulong lastProcessedCommandId = 0)
        {
            return new PlayerSnapshot(
                revision,
                turnId,
                NetworkPlayerSlot.First,
                NetworkPlayerSlot.First,
                NetworkPlayerSlot.None,
                100.0,
                lastProcessedCommandId,
                new sbyte[36],
                new NetworkShotState[36],
                new NetworkShotState[36],
                new int[4],
                new bool[4]);
        }

        private ClientMessage DecodeClient(byte[] payload)
        {
            bool wasDecoded = _serializer.TryDeserializeClient(
                payload,
                out ClientMessage message,
                out ProtocolError error);
            Assert.That(wasDecoded, Is.True, error.ToString());

            return message;
        }
    }
}
