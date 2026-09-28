using System;
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
    public sealed class GameClientTests
    {
        private static readonly TransportConnectionId ServerConnection = new TransportConnectionId(0);
        private static readonly ClientRecoverySettings RecoverySettings =
            new ClientRecoverySettings(2.0, 5.0, 1.0);

        private FakeMessageTransport _transport;
        private FakeClientIdentityStore _identityStore;
        private IProtocolSerializer _serializer;
        private GameClient _client;

        [SetUp]
        public void SetUp()
        {
            _transport = new FakeMessageTransport();
            _identityStore = new FakeClientIdentityStore(Guid.NewGuid(), 50);
            _serializer = new BinaryProtocolSerializer();
            _client = new GameClient(
                _transport,
                _identityStore,
                ServerConnection,
                _serializer,
                RecoverySettings);
            _client.Tick(100.0);
            _client.Connect();
            _client.Receive(_serializer.Serialize(CreateSessionAccepted(CreateSnapshot(1, 10))));
            _transport.Clear();
        }

        [Test]
        public void TryFire_WhenCommandIsPending_BlocksSecondClick()
        {
            // Arrange
            const int firstCellIndex = 3;
            const int secondCellIndex = 4;

            // Act
            bool wasFirstAccepted = _client.TryFire(firstCellIndex);
            bool wasSecondAccepted = _client.TryFire(secondCellIndex);

            // Assert
            Assert.That(wasFirstAccepted, Is.True);
            Assert.That(wasSecondAccepted, Is.False);
            Assert.That(_client.hasPendingCommand, Is.True);
            Assert.That(_transport.sentMessages, Has.Count.EqualTo(1));
            ClientMessage sent = DecodeClient(_transport.sentMessages[0].payload);
            Assert.That(sent.cellIndex, Is.EqualTo(firstCellIndex));
        }

        [Test]
        public void Receive_WhenFireResultCommandDoesNotMatch_KeepsPendingCommand()
        {
            // Arrange
            _client.TryFire(3);
            ClientMessage request = DecodeClient(_transport.sentMessages[0].payload);
            ServerMessage unrelatedResult = ServerMessage.CreateFireResult(
                10,
                request.commandId + 1,
                RequestStatus.Accepted,
                RejectionReason.None,
                NetworkShotResult.Miss,
                0,
                CreateSnapshot(2, 11),
                100.0);

            // Act
            _client.Receive(_serializer.Serialize(unrelatedResult));

            // Assert
            Assert.That(_client.hasPendingCommand, Is.True);
        }

        [Test]
        public void Receive_WhenFireResultCommandMatches_ClearsPendingCommand()
        {
            // Arrange
            _client.TryFire(3);
            ClientMessage request = DecodeClient(_transport.sentMessages[0].payload);
            ServerMessage result = ServerMessage.CreateFireResult(
                10,
                request.commandId,
                RequestStatus.Rejected,
                RejectionReason.InvalidTarget,
                NetworkShotResult.Invalid,
                0,
                CreateSnapshot(1, 10),
                100.0);

            // Act
            _client.Receive(_serializer.Serialize(result));

            // Assert
            Assert.That(_client.hasPendingCommand, Is.False);
        }

        [Test]
        public void Receive_WhenSnapshotRevisionIsNotNewer_DoesNotRollStateBack()
        {
            // Arrange
            PlayerSnapshot current = CreateSnapshot(3, 30);
            PlayerSnapshot older = CreateSnapshot(2, 20);
            PlayerSnapshot equal = CreateSnapshot(3, 99);
            _client.Receive(_serializer.Serialize(ServerMessage.CreateStateSnapshot(20, current, 100.0)));

            // Act
            _client.Receive(_serializer.Serialize(ServerMessage.CreateStateSnapshot(21, older, 100.0)));
            _client.Receive(_serializer.Serialize(ServerMessage.CreateStateSnapshot(22, equal, 100.0)));

            // Assert
            Assert.That(_client.snapshot.revision, Is.EqualTo(3));
            Assert.That(_client.snapshot.turnId, Is.EqualTo(30));
        }

        [Test]
        public void Receive_WhenResumeIsAccepted_AppliesFullSnapshotWithEqualRevision()
        {
            // Arrange
            _client.Receive(_serializer.Serialize(
                ServerMessage.CreateStateSnapshot(20, CreateSnapshot(4, 40), 100.0)));
            _client.Connect();
            ClientMessage resumeRequest = DecodeClient(_transport.sentMessages[^1].payload);
            PlayerSnapshot resumedSnapshot = CreateSnapshot(4, 41);

            // Act
            _client.Receive(_serializer.Serialize(CreateSessionAccepted(resumedSnapshot)));

            // Assert
            Assert.That(resumeRequest.type, Is.EqualTo(MessageType.ResumeRequest));
            Assert.That(resumeRequest.clientId, Is.EqualTo(_identityStore.clientId));
            Assert.That(_client.snapshot.turnId, Is.EqualTo(41));
            Assert.That(_client.connectionState, Is.EqualTo(ClientConnectionState.Connected));
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
            var ownShipIndices = new sbyte[36];
            var ownShots = new NetworkShotState[36];
            var opponentShots = new NetworkShotState[36];
            var hitCounts = new int[4];
            var sunkStates = new bool[4];

            return new PlayerSnapshot(
                revision,
                turnId,
                NetworkPlayerSlot.First,
                NetworkPlayerSlot.First,
                NetworkPlayerSlot.None,
                100.0,
                lastProcessedCommandId,
                ownShipIndices,
                ownShots,
                opponentShots,
                hitCounts,
                sunkStates);
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

        private sealed class FakeClientIdentityStore : IClientIdentityStore
        {
            private ulong _nextCommandId;

            public Guid clientId { get; }

            public FakeClientIdentityStore(Guid clientId, ulong nextCommandId)
            {
                this.clientId = clientId;
                _nextCommandId = nextCommandId;
            }

            public ulong ReserveCommandId()
            {
                return _nextCommandId++;
            }
        }
    }
}
