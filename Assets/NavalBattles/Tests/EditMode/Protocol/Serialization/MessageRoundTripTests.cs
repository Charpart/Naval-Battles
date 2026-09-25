using System;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Serialization;
using NavalBattles.Runtime.Protocol.Snapshots;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Protocol.Serialization
{
    [TestFixture]
    public sealed class MessageRoundTripTests
    {
        private readonly IProtocolSerializer _serializer = new BinaryProtocolSerializer();

        [TestCase(MessageType.ConnectRequest)]
        [TestCase(MessageType.ResumeRequest)]
        [TestCase(MessageType.StateRequest)]
        [TestCase(MessageType.HeartbeatRequest)]
        public void ClientMessage_AfterRoundTrip_PreservesHeaderAndPayload(MessageType type)
        {
            // Arrange
            Guid clientId = Guid.Parse("4188d7c5-7ebf-4be8-bf9a-6112946f51f8");
            ClientMessage source = CreateClientMessage(type, clientId);

            // Act
            byte[] payload = _serializer.Serialize(source);
            bool wasDecoded = _serializer.TryDeserializeClient(
                payload,
                out ClientMessage decoded,
                out ProtocolError error);

            // Assert
            Assert.That(wasDecoded, Is.True, error.ToString());
            Assert.That(decoded.type, Is.EqualTo(type));
            Assert.That(decoded.clientId, Is.EqualTo(clientId));
            Assert.That(decoded.messageId, Is.EqualTo(15));
            Assert.That(decoded.knownRevision, Is.EqualTo(source.knownRevision));
        }

        [TestCase(MessageType.FireResult)]
        [TestCase(MessageType.StateSnapshot)]
        [TestCase(MessageType.HeartbeatResponse)]
        [TestCase(MessageType.RequestRejected)]
        public void ServerMessage_AfterRoundTrip_PreservesHeaderAndPayload(MessageType type)
        {
            // Arrange
            ServerMessage source = CreateServerMessage(type);

            // Act
            byte[] payload = _serializer.Serialize(source);
            bool wasDecoded = _serializer.TryDeserializeServer(
                payload,
                out ServerMessage decoded,
                out ProtocolError error);

            // Assert
            Assert.That(wasDecoded, Is.True, error.ToString());
            Assert.That(decoded.type, Is.EqualTo(type));
            Assert.That(decoded.messageId, Is.EqualTo(28));
            Assert.That(decoded.commandId, Is.EqualTo(source.commandId));
            Assert.That(decoded.requestStatus, Is.EqualTo(source.requestStatus));
            Assert.That(decoded.rejectionReason, Is.EqualTo(source.rejectionReason));
            Assert.That(decoded.shotResult, Is.EqualTo(source.shotResult));
            Assert.That(decoded.serverTime, Is.EqualTo(source.serverTime));
            Assert.That(decoded.snapshot?.revision, Is.EqualTo(source.snapshot?.revision));
        }

        private static ClientMessage CreateClientMessage(MessageType type, Guid clientId)
        {
            switch (type)
            {
                case MessageType.ConnectRequest:
                    return ClientMessage.CreateConnectRequest(clientId, 15);

                case MessageType.ResumeRequest:
                    return ClientMessage.CreateResumeRequest(clientId, 15, 7);

                case MessageType.StateRequest:
                    return ClientMessage.CreateStateRequest(clientId, 15, 7);

                case MessageType.HeartbeatRequest:
                    return ClientMessage.CreateHeartbeatRequest(clientId, 15);

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static ServerMessage CreateServerMessage(MessageType type)
        {
            PlayerSnapshot snapshot = CreateSnapshot();

            switch (type)
            {
                case MessageType.FireResult:
                    return ServerMessage.CreateFireResult(
                        28,
                        9,
                        RequestStatus.Accepted,
                        RejectionReason.None,
                        NetworkShotResult.Sunk,
                        2,
                        snapshot);

                case MessageType.StateSnapshot:
                    return ServerMessage.CreateStateSnapshot(28, snapshot);

                case MessageType.HeartbeatResponse:
                    return ServerMessage.CreateHeartbeatResponse(28, 42.5);

                case MessageType.RequestRejected:
                    return ServerMessage.CreateRequestRejected(28, 9, RejectionReason.WrongTurn);

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static PlayerSnapshot CreateSnapshot()
        {
            return new PlayerSnapshot(
                7,
                3,
                NetworkPlayerSlot.First,
                NetworkPlayerSlot.Second,
                NetworkPlayerSlot.None,
                42.5,
                9,
                new sbyte[] { 0, 0, -1, -1 },
                new[] { NetworkShotState.Hit, NetworkShotState.None, NetworkShotState.None, NetworkShotState.Miss },
                new[] { NetworkShotState.None, NetworkShotState.Miss, NetworkShotState.Hit, NetworkShotState.None },
                new[] { 1 },
                new[] { false });
        }

    }
}
