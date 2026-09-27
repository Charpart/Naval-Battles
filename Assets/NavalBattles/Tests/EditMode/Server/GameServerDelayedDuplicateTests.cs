using System;
using NavalBattles.Runtime.Domain.Configuration;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Serialization;
using NavalBattles.Runtime.Server;
using NavalBattles.Runtime.Transport;
using NavalBattles.Tests.EditMode.Fakes;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Server
{
    [TestFixture]
    public sealed class GameServerDelayedDuplicateTests
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
        public void Receive_WhenOlderCommandIsDuplicatedAfterNewerCommand_RepeatsOriginalResult()
        {
            // Arrange
            Guid firstClientId = Guid.NewGuid();
            Guid secondClientId = Guid.NewGuid();
            var firstConnection = new TransportConnectionId(10);
            var secondConnection = new TransportConnectionId(20);
            Connect(firstClientId, firstConnection, 1);
            Connect(secondClientId, secondConnection, 2);
            ServerMessage firstSession = DecodeLastFor(firstConnection);
            bool isFirstConnectionActive = firstSession.snapshot.activePlayer == firstSession.player;
            Guid activeClientId = isFirstConnectionActive ? firstClientId : secondClientId;
            Guid otherClientId = isFirstConnectionActive ? secondClientId : firstClientId;
            TransportConnectionId activeConnection = isFirstConnectionActive ? firstConnection : secondConnection;
            TransportConnectionId otherConnection = isFirstConnectionActive ? secondConnection : firstConnection;
            byte[] originalPayload = CreateFirePayload(activeClientId, 10, 77, 1, 0);
            _server.Receive(activeConnection, originalPayload, SERVER_TIME + 1.0);
            ServerMessage originalResult = DecodeLastFor(activeConnection);
            SendFire(otherClientId, otherConnection, 11, 78, 2, 0, SERVER_TIME + 2.0);
            SendFire(activeClientId, activeConnection, 12, 79, 3, 1, SERVER_TIME + 3.0);
            ulong revisionBeforeDuplicate = _server.stateRevision;
            _transport.Clear();

            // Act
            _server.Receive(activeConnection, originalPayload, SERVER_TIME + 4.0);

            // Assert
            ServerMessage repeatedResult = DecodeLastFor(activeConnection);
            Assert.That(_server.stateRevision, Is.EqualTo(revisionBeforeDuplicate));
            Assert.That(repeatedResult.commandId, Is.EqualTo(77));
            Assert.That(repeatedResult.requestStatus, Is.EqualTo(originalResult.requestStatus));
            Assert.That(repeatedResult.shotResult, Is.EqualTo(originalResult.shotResult));
            Assert.That(repeatedResult.snapshot.revision, Is.EqualTo(originalResult.snapshot.revision));
        }

        private void Connect(Guid clientId, TransportConnectionId connectionId, ulong messageId)
        {
            ClientMessage request = ClientMessage.CreateConnectRequest(clientId, messageId);
            _server.Receive(connectionId, _serializer.Serialize(request), SERVER_TIME);
        }

        private void SendFire(
            Guid clientId,
            TransportConnectionId connectionId,
            ulong messageId,
            ulong commandId,
            ulong turnId,
            int cellIndex,
            double serverTime)
        {
            byte[] payload = CreateFirePayload(clientId, messageId, commandId, turnId, cellIndex);
            _server.Receive(connectionId, payload, serverTime);
        }

        private byte[] CreateFirePayload(
            Guid clientId,
            ulong messageId,
            ulong commandId,
            ulong turnId,
            int cellIndex)
        {
            ClientMessage request = ClientMessage.CreateFireRequest(
                clientId,
                messageId,
                commandId,
                turnId,
                cellIndex);

            return _serializer.Serialize(request);
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
