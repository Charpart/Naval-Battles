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
    public sealed class GameClientRecoveryEdgeCaseTests
    {
        private static readonly TransportConnectionId ServerConnection =
            new TransportConnectionId(0);
        private static readonly ClientRecoverySettings RecoverySettings =
            new ClientRecoverySettings(2.0, 6.0, 1.0);

        private FakeMessageTransport _transport;
        private IProtocolSerializer _serializer;
        private GameClient _client;

        [SetUp]
        public void SetUp()
        {
            _transport = new FakeMessageTransport();
            _serializer = new BinaryProtocolSerializer();
            _client = CreateClient();
            _client.Tick(100.0);
        }

        [Test]
        public void Tick_WhenConnectIsUnconfirmed_RetriesSameConnectRequest()
        {
            // Arrange
            _client.Connect();
            ClientMessage firstRequest = DecodeClient(_transport.sentMessages.Single().payload);
            _transport.Clear();
            _client.Receive(_serializer.Serialize(ServerMessage.CreateHeartbeatResponse(1, 100.5)));

            // Act
            _client.Tick(101.1);

            // Assert
            ClientMessage retry = DecodeClient(_transport.sentMessages.Single().payload);
            Assert.That(retry.type, Is.EqualTo(MessageType.ConnectRequest));
            Assert.That(retry.messageId, Is.EqualTo(firstRequest.messageId));
        }

        [Test]
        public void Tick_WhenResumeIsUnconfirmed_RetriesSameResumeRequest()
        {
            // Arrange
            AcceptSession(CreateSnapshot(5, 10));
            _transport.Clear();
            _client.Connect();
            ClientMessage firstRequest = DecodeClient(_transport.sentMessages.Single().payload);
            _transport.Clear();

            // Act
            _client.Tick(101.1);

            // Assert
            ClientMessage retry = DecodeClient(_transport.sentMessages.Single().payload);
            Assert.That(retry.type, Is.EqualTo(MessageType.ResumeRequest));
            Assert.That(retry.messageId, Is.EqualTo(firstRequest.messageId));
        }

        [Test]
        public void Receive_WhenSessionAcceptedIsOlder_DoesNotRollbackSnapshot()
        {
            // Arrange
            AcceptSession(CreateSnapshot(9, 20));

            // Act
            AcceptSession(CreateSnapshot(1, 1));

            // Assert
            Assert.That(_client.snapshot.revision, Is.EqualTo(9));
            Assert.That(_client.snapshot.turnId, Is.EqualTo(20));
        }

        [Test]
        public void Tick_WhenConnected_RequestsCurrentStateWithKnownRevision()
        {
            // Arrange
            AcceptSession(CreateSnapshot(7, 20));
            _transport.Clear();

            // Act
            _client.Tick(102.1);

            // Assert
            ClientMessage stateRequest = _transport.sentMessages
                .Select(message => DecodeClient(message.payload))
                .Single(message => message.type == MessageType.StateRequest);
            Assert.That(stateRequest.knownRevision, Is.EqualTo(7));
        }

        [Test]
        public void Receive_WhenFireIsRejected_StoresReasonForPresentation()
        {
            // Arrange
            AcceptSession(CreateSnapshot(7, 20));
            Assert.That(_client.TryFire(3), Is.True);
            ClientMessage fire = DecodeClient(_transport.sentMessages[^1].payload);
            ServerMessage rejection = ServerMessage.CreateFireResult(
                10,
                fire.commandId,
                RequestStatus.Rejected,
                RejectionReason.TurnExpired,
                NetworkShotResult.Invalid,
                0,
                CreateSnapshot(7, 20, fire.commandId));

            // Act
            _client.Receive(_serializer.Serialize(rejection));

            // Assert
            Assert.That(_client.lastRequestStatus, Is.EqualTo(RequestStatus.Rejected));
            Assert.That(_client.lastRejectionReason, Is.EqualTo(RejectionReason.TurnExpired));
        }

        private GameClient CreateClient()
        {
            return new GameClient(
                _transport,
                new ClientIdentityStore(Guid.NewGuid()),
                ServerConnection,
                _serializer,
                RecoverySettings);
        }

        private void AcceptSession(PlayerSnapshot snapshot)
        {
            int[] shipLengths = { 3, 2, 2, 1 };
            var definition = new GameDefinition(6, 6, shipLengths, 15.0);
            ServerMessage accepted = ServerMessage.CreateSessionAccepted(
                1,
                NetworkPlayerSlot.First,
                definition,
                snapshot);
            _client.Receive(_serializer.Serialize(accepted));
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
                115.0,
                lastProcessedCommandId,
                new sbyte[36],
                new NetworkShotState[36],
                new NetworkShotState[36],
                new int[4],
                new bool[4]);
        }
    }
}
