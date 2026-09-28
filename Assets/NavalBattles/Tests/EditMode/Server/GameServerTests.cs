using System;
using System.Collections.Generic;
using NavalBattles.Runtime.Domain.Configuration;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Serialization;
using NavalBattles.Runtime.Protocol.Snapshots;
using NavalBattles.Runtime.Server;
using NavalBattles.Runtime.Transport;
using NavalBattles.Tests.EditMode.Fakes;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Server
{
    [TestFixture]
    public sealed class GameServerTests
    {
        private const double SERVER_TIME = 100.0;

        private FakeMessageTransport _transport;
        private IProtocolSerializer _serializer;
        private GameServer _server;

        [SetUp]
        public void SetUp()
        {
            int[] shipLengths = { 3, 2, 2, 1 };
            GameRules.TryCreate(6, 6, shipLengths, 15.0, out GameRules rules, out GameRulesValidationError error);
            Assert.That(error, Is.EqualTo(GameRulesValidationError.None));
            _transport = new FakeMessageTransport();
            _serializer = new BinaryProtocolSerializer();
            _server = new GameServer(rules, 42, SERVER_TIME, _transport, _serializer);
        }

        [Test]
        public void Receive_WhenTwoClientsConnect_AssignsDifferentPlayerSlots()
        {
            // Arrange
            Guid firstClientId = Guid.NewGuid();
            Guid secondClientId = Guid.NewGuid();
            var firstConnection = new TransportConnectionId(10);
            var secondConnection = new TransportConnectionId(20);

            // Act
            Connect(firstClientId, firstConnection, 1);
            Connect(secondClientId, secondConnection, 2);

            // Assert
            ServerMessage firstResponse = DecodeLastFor(firstConnection);
            ServerMessage secondResponse = DecodeLastFor(secondConnection);
            Assert.That(firstResponse.type, Is.EqualTo(MessageType.SessionAccepted));
            Assert.That(secondResponse.type, Is.EqualTo(MessageType.SessionAccepted));
            Assert.That(secondResponse.player, Is.Not.EqualTo(firstResponse.player));
            Assert.That(firstResponse.snapshot.ownShipIndices, Has.Some.GreaterThanOrEqualTo(0));
            Assert.That(firstResponse.snapshot.opponentShots, Has.All.EqualTo(NetworkShotState.None));
        }

        [Test]
        public void Receive_WhenThirdClientConnects_RejectsServerFull()
        {
            // Arrange
            Connect(Guid.NewGuid(), new TransportConnectionId(10), 1);
            Connect(Guid.NewGuid(), new TransportConnectionId(20), 2);
            var thirdConnection = new TransportConnectionId(30);

            // Act
            Connect(Guid.NewGuid(), thirdConnection, 3);

            // Assert
            ServerMessage response = DecodeLastFor(thirdConnection);
            Assert.That(response.type, Is.EqualTo(MessageType.RequestRejected));
            Assert.That(response.rejectionReason, Is.EqualTo(RejectionReason.ServerFull));
        }

        [Test]
        public void Receive_WhenOnlyOneClientConnects_WaitsForOpponent()
        {
            // Act
            Connect(Guid.NewGuid(), new TransportConnectionId(10), 1);

            // Assert
            Assert.That(_transport.sentMessages, Is.Empty);
        }

        [Test]
        public void Receive_WhenSecondClientConnectsLate_StartsFreshTurnDeadline()
        {
            // Arrange
            Guid firstClientId = Guid.NewGuid();
            Guid secondClientId = Guid.NewGuid();
            var firstConnection = new TransportConnectionId(10);
            var secondConnection = new TransportConnectionId(20);
            Connect(firstClientId, firstConnection, 1, SERVER_TIME);
            Connect(secondClientId, secondConnection, 2, SERVER_TIME + 100.0);
            ServerMessage firstSession = DecodeLastFor(firstConnection);
            ServerMessage secondSession = DecodeLastFor(secondConnection);
            bool isFirstActive = firstSession.snapshot.activePlayer == firstSession.player;
            Guid activeClientId = isFirstActive ? firstClientId : secondClientId;
            TransportConnectionId activeConnection = isFirstActive ? firstConnection : secondConnection;
            ulong turnId = isFirstActive ? firstSession.snapshot.turnId : secondSession.snapshot.turnId;
            ClientMessage request = ClientMessage.CreateFireRequest(activeClientId, 3, 77, turnId, 0);

            // Act
            _server.Receive(activeConnection, _serializer.Serialize(request), SERVER_TIME + 114.0);

            // Assert
            ServerMessage result = DecodeLastFor(activeConnection);
            Assert.That(result.type, Is.EqualTo(MessageType.FireResult));
            Assert.That(result.requestStatus, Is.EqualTo(RequestStatus.Accepted));
        }

        [Test]
        public void Receive_WhenSessionIsAccepted_IncludesCurrentServerTime()
        {
            // Arrange
            var firstConnection = new TransportConnectionId(10);
            var secondConnection = new TransportConnectionId(20);
            Connect(Guid.NewGuid(), firstConnection, 1, SERVER_TIME);

            // Act
            Connect(Guid.NewGuid(), secondConnection, 2, SERVER_TIME + 100.0);

            // Assert
            ServerMessage response = DecodeLastFor(firstConnection);
            Assert.That(response.serverTime, Is.EqualTo(SERVER_TIME + 100.0));
        }

        [Test]
        public void Receive_WhenHeartbeatArrives_EchoesRequestMessageId()
        {
            // Arrange
            var connection = new TransportConnectionId(10);
            ClientMessage heartbeat = ClientMessage.CreateHeartbeatRequest(Guid.NewGuid(), 77);

            // Act
            _server.Receive(connection, _serializer.Serialize(heartbeat), SERVER_TIME);

            // Assert
            ServerMessage response = DecodeLastFor(connection);
            Assert.That(response.type, Is.EqualTo(MessageType.HeartbeatResponse));
            Assert.That(response.messageId, Is.EqualTo(heartbeat.messageId));
        }

        [Test]
        public void Receive_WhenFireRequestIsDuplicated_ChangesMatchExactlyOnceAndRepeatsResult()
        {
            // Arrange
            Guid firstClientId = Guid.NewGuid();
            Guid secondClientId = Guid.NewGuid();
            var firstConnection = new TransportConnectionId(10);
            var secondConnection = new TransportConnectionId(20);
            Connect(firstClientId, firstConnection, 1);
            Connect(secondClientId, secondConnection, 2);
            ServerMessage firstSession = DecodeFirstOfType(firstConnection, MessageType.SessionAccepted);
            ServerMessage secondSession = DecodeFirstOfType(secondConnection, MessageType.SessionAccepted);
            bool isFirstActive = firstSession.snapshot.activePlayer == firstSession.player;
            Guid activeClientId = isFirstActive ? firstClientId : secondClientId;
            TransportConnectionId activeConnection = isFirstActive ? firstConnection : secondConnection;
            ulong turnId = isFirstActive ? firstSession.snapshot.turnId : secondSession.snapshot.turnId;
            ClientMessage fireRequest = ClientMessage.CreateFireRequest(activeClientId, 3, 77, turnId, 0);
            byte[] payload = _serializer.Serialize(fireRequest);
            _transport.Clear();

            // Act
            _server.Receive(activeConnection, payload, SERVER_TIME + 1.0);
            ulong revisionAfterFirstRequest = _server.stateRevision;
            _server.Receive(activeConnection, payload, SERVER_TIME + 2.0);

            // Assert
            Assert.That(_server.stateRevision, Is.EqualTo(revisionAfterFirstRequest));
            List<ServerMessage> fireResults = DecodeAllFor(activeConnection, MessageType.FireResult);
            Assert.That(fireResults, Has.Count.EqualTo(2));
            Assert.That(fireResults[0].commandId, Is.EqualTo(77));
            Assert.That(fireResults[1].commandId, Is.EqualTo(77));
            Assert.That(fireResults[1].snapshot.revision, Is.EqualTo(fireResults[0].snapshot.revision));
            Assert.That(fireResults[1].shotResult, Is.EqualTo(fireResults[0].shotResult));
        }

        [Test]
        public void Receive_WhenClientResumesWithNewConnection_RebindsExistingPlayerSlot()
        {
            // Arrange
            Guid clientId = Guid.NewGuid();
            var initialConnection = new TransportConnectionId(10);
            var recreatedConnection = new TransportConnectionId(99);
            Connect(clientId, initialConnection, 1);
            Connect(Guid.NewGuid(), new TransportConnectionId(20), 2);
            ServerMessage initialSession = DecodeLastFor(initialConnection);
            ClientMessage resume = ClientMessage.CreateResumeRequest(clientId, 3, initialSession.snapshot.revision);

            // Act
            _server.Receive(recreatedConnection, _serializer.Serialize(resume), SERVER_TIME + 1.0);

            // Assert
            ServerMessage resumedSession = DecodeLastFor(recreatedConnection);
            Assert.That(resumedSession.type, Is.EqualTo(MessageType.SessionAccepted));
            Assert.That(resumedSession.player, Is.EqualTo(initialSession.player));
            Assert.That(_server.connectedPlayerCount, Is.EqualTo(2));
        }

        [Test]
        public void GetPlayerSnapshot_ForBothPlayers_ReturnsEachCompleteOwnBoard()
        {
            // Arrange
            Connect(Guid.NewGuid(), new TransportConnectionId(10), 1);
            Connect(Guid.NewGuid(), new TransportConnectionId(20), 2);

            // Act
            PlayerSnapshot first = _server.GetPlayerSnapshot(NetworkPlayerSlot.First);
            PlayerSnapshot second = _server.GetPlayerSnapshot(NetworkPlayerSlot.Second);

            // Assert
            Assert.That(first.player, Is.EqualTo(NetworkPlayerSlot.First));
            Assert.That(second.player, Is.EqualTo(NetworkPlayerSlot.Second));
            Assert.That(first.ownShipIndices, Has.Some.GreaterThanOrEqualTo(0));
            Assert.That(second.ownShipIndices, Has.Some.GreaterThanOrEqualTo(0));
            Assert.That(first.ownShipIndices, Has.Count.EqualTo(36));
            Assert.That(second.ownShipIndices, Has.Count.EqualTo(36));
        }

        [Test]
        public void HasPlayer_BeforeAndAfterConnections_ReportsOccupiedSlots()
        {
            // Assert
            Assert.That(_server.HasPlayer(NetworkPlayerSlot.First), Is.False);
            Assert.That(_server.HasPlayer(NetworkPlayerSlot.Second), Is.False);

            // Act
            Connect(Guid.NewGuid(), new TransportConnectionId(10), 1);

            // Assert
            Assert.That(_server.HasPlayer(NetworkPlayerSlot.First), Is.True);
            Assert.That(_server.HasPlayer(NetworkPlayerSlot.Second), Is.False);

            // Act
            Connect(Guid.NewGuid(), new TransportConnectionId(20), 2);

            // Assert
            Assert.That(_server.HasPlayer(NetworkPlayerSlot.Second), Is.True);
        }

        [TestCase(NetworkPlayerSlot.None)]
        [TestCase((NetworkPlayerSlot)255)]
        public void GetPlayerSnapshot_WithInvalidSlot_ThrowsArgumentOutOfRangeException(
            NetworkPlayerSlot player)
        {
            // Act
            TestDelegate getSnapshot = () => _server.GetPlayerSnapshot(player);

            // Assert
            Assert.That(getSnapshot, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private void Connect(Guid clientId, TransportConnectionId connectionId, ulong messageId)
        {
            Connect(clientId, connectionId, messageId, SERVER_TIME);
        }

        private void Connect(
            Guid clientId,
            TransportConnectionId connectionId,
            ulong messageId,
            double serverTime)
        {
            ClientMessage request = ClientMessage.CreateConnectRequest(clientId, messageId);
            _server.Receive(connectionId, _serializer.Serialize(request), serverTime);
        }

        private ServerMessage DecodeLastFor(TransportConnectionId connectionId)
        {
            for (int index = _transport.sentMessages.Count - 1; index >= 0; index--)
            {
                SentMessage sentMessage = _transport.sentMessages[index];

                if (sentMessage.target == connectionId)
                {
                    return Decode(sentMessage.payload);
                }
            }

            Assert.Fail($"No message for connection {connectionId.value}");

            return default;
        }

        private ServerMessage DecodeFirstOfType(TransportConnectionId connectionId, MessageType type)
        {
            for (int index = 0; index < _transport.sentMessages.Count; index++)
            {
                SentMessage sentMessage = _transport.sentMessages[index];

                if (sentMessage.target != connectionId)
                {
                    continue;
                }

                ServerMessage message = Decode(sentMessage.payload);

                if (message.type == type)
                {
                    return message;
                }
            }

            Assert.Fail($"No {type} message for connection {connectionId.value}");

            return default;
        }

        private List<ServerMessage> DecodeAllFor(TransportConnectionId connectionId, MessageType type)
        {
            var messages = new List<ServerMessage>();

            for (int index = 0; index < _transport.sentMessages.Count; index++)
            {
                SentMessage sentMessage = _transport.sentMessages[index];

                if (sentMessage.target != connectionId)
                {
                    continue;
                }

                ServerMessage message = Decode(sentMessage.payload);

                if (message.type == type)
                {
                    messages.Add(message);
                }
            }

            return messages;
        }

        private ServerMessage Decode(byte[] payload)
        {
            bool wasDecoded = _serializer.TryDeserializeServer(
                payload,
                out ServerMessage message,
                out ProtocolError error);
            Assert.That(wasDecoded, Is.True, error.ToString());

            return message;
        }
    }
}
