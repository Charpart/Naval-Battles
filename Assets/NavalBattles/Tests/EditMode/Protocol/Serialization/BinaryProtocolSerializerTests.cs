using System;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Serialization;
using NavalBattles.Runtime.Protocol.Snapshots;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Protocol.Serialization
{
    [TestFixture]
    public sealed class BinaryProtocolSerializerTests
    {
        private readonly IProtocolSerializer _serializer = new BinaryProtocolSerializer();

        [Test]
        public void FireRequestFactory_CreatesCompactMessage()
        {
            // Act
            ClientMessage message = ClientMessage.CreateFireRequest(Guid.NewGuid(), 15, 9, 4, 27);

            // Assert
            Assert.That(message.type, Is.EqualTo(MessageType.FireRequest));
            Assert.That(message.commandId, Is.EqualTo(9));
            Assert.That(message.turnId, Is.EqualTo(4));
            Assert.That(message.cellIndex, Is.EqualTo(27));
        }

        [Test]
        public void BinaryProtocolSerializer_ThroughInterface_RoundTripsMessage()
        {
            // Arrange
            ClientMessage source = ClientMessage.CreateHeartbeatRequest(Guid.NewGuid(), 1);

            // Act
            byte[] payload = _serializer.Serialize(source);
            bool wasDecoded = _serializer.TryDeserializeClient(
                payload,
                out ClientMessage decoded,
                out ProtocolError error);

            // Assert
            Assert.That(wasDecoded, Is.True, error.ToString());
            Assert.That(decoded.clientId, Is.EqualTo(source.clientId));
        }

        [Test]
        public void SerializeClient_WritesClientIdAndPayloadLengthToHeader()
        {
            // Arrange
            Guid clientId = Guid.Parse("4188d7c5-7ebf-4be8-bf9a-6112946f51f8");
            ClientMessage message = ClientMessage.CreateFireRequest(clientId, 15, 9, 4, 27);

            // Act
            byte[] bytes = _serializer.Serialize(message);

            // Assert
            byte[] encodedClientId = new byte[16];
            Array.Copy(bytes, 10, encodedClientId, 0, encodedClientId.Length);
            int payloadLength = ReadInt32LittleEndian(bytes, 26);
            Assert.That(bytes[0], Is.EqualTo(1));
            Assert.That(new Guid(encodedClientId), Is.EqualTo(clientId));
            Assert.That(payloadLength, Is.EqualTo(bytes.Length - 30));
        }

        [Test]
        public void TryDeserializeClient_WithMismatchedPayloadLength_ReturnsInvalidPayloadLength()
        {
            // Arrange
            ClientMessage message = ClientMessage.CreateFireRequest(Guid.NewGuid(), 1, 2, 3, 4);
            byte[] bytes = _serializer.Serialize(message);
            bytes[26]--;

            // Act
            bool wasDecoded = _serializer.TryDeserializeClient(
                bytes,
                out ClientMessage decoded,
                out ProtocolError error);

            // Assert
            Assert.That(wasDecoded, Is.False);
            Assert.That(decoded.type, Is.EqualTo(MessageType.None));
            Assert.That(error, Is.EqualTo(ProtocolError.InvalidPayloadLength));
        }

        [Test]
        public void StateSnapshot_AfterRoundTrip_PreservesPrivateAndPublicViews()
        {
            // Arrange
            var definition = new GameDefinition(2, 2, new[] { 2, 1 }, 15.0);
            var snapshot = new PlayerSnapshot(
                7,
                3,
                NetworkPlayerSlot.First,
                NetworkPlayerSlot.Second,
                NetworkPlayerSlot.None,
                42.5,
                11,
                new sbyte[] { 0, 0, 1, -1 },
                new[] { NetworkShotState.Hit, NetworkShotState.None, NetworkShotState.None, NetworkShotState.Miss },
                new[] { NetworkShotState.None, NetworkShotState.Miss, NetworkShotState.Hit, NetworkShotState.None },
                new[] { 1, 0 },
                new[] { false, true });
            ServerMessage source = ServerMessage.CreateSessionAccepted(
                28,
                NetworkPlayerSlot.First,
                definition,
                snapshot);

            // Act
            byte[] payload = _serializer.Serialize(source);
            bool wasDecoded = _serializer.TryDeserializeServer(
                payload,
                out ServerMessage decoded,
                out ProtocolError error);

            // Assert
            Assert.That(wasDecoded, Is.True);
            Assert.That(error, Is.EqualTo(ProtocolError.None));
            Assert.That(decoded.type, Is.EqualTo(MessageType.SessionAccepted));
            Assert.That(decoded.player, Is.EqualTo(NetworkPlayerSlot.First));
            Assert.That(decoded.definition.width, Is.EqualTo(2));
            Assert.That(decoded.definition.shipLengths, Is.EqualTo(new[] { 2, 1 }));
            Assert.That(decoded.snapshot.revision, Is.EqualTo(7));
            Assert.That(decoded.snapshot.lastProcessedCommandId, Is.EqualTo(11));
            Assert.That(decoded.snapshot.ownShipIndices, Is.EqualTo(new sbyte[] { 0, 0, 1, -1 }));
            Assert.That(
                decoded.snapshot.opponentShots,
                Is.EqualTo(new[] { NetworkShotState.None, NetworkShotState.Miss, NetworkShotState.Hit, NetworkShotState.None }));
            Assert.That(decoded.snapshot.opponentShipsSunk, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void TryDeserializeClient_WithUnsupportedVersion_ReturnsUnsupportedVersion()
        {
            // Arrange
            ClientMessage message = ClientMessage.CreateHeartbeatRequest(Guid.NewGuid(), 1);
            byte[] payload = _serializer.Serialize(message);
            payload[0] = 99;

            // Act
            bool wasDecoded = _serializer.TryDeserializeClient(
                payload,
                out ClientMessage decoded,
                out ProtocolError error);

            // Assert
            Assert.That(wasDecoded, Is.False);
            Assert.That(decoded.type, Is.EqualTo(MessageType.None));
            Assert.That(error, Is.EqualTo(ProtocolError.UnsupportedVersion));
        }

        [Test]
        public void TryDeserializeClient_WithUnknownType_ReturnsUnknownMessageType()
        {
            // Arrange
            byte[] payload = _serializer.Serialize(
                ClientMessage.CreateHeartbeatRequest(Guid.NewGuid(), 1));
            payload[1] = 99;

            // Act
            bool wasDecoded = _serializer.TryDeserializeClient(
                payload,
                out ClientMessage decoded,
                out ProtocolError error);

            // Assert
            Assert.That(wasDecoded, Is.False);
            Assert.That(decoded.type, Is.EqualTo(MessageType.None));
            Assert.That(error, Is.EqualTo(ProtocolError.UnknownMessageType));
        }

        [Test]
        public void TryDeserializeServer_WithTruncatedPayload_ReturnsUnexpectedEnd()
        {
            // Arrange
            ServerMessage message = ServerMessage.CreateHeartbeatResponse(1, 123.0);
            byte[] payload = _serializer.Serialize(message);
            Array.Resize(ref payload, payload.Length - 1);

            // Act
            bool wasDecoded = _serializer.TryDeserializeServer(
                payload,
                out ServerMessage decoded,
                out ProtocolError error);

            // Assert
            Assert.That(wasDecoded, Is.False);
            Assert.That(decoded.type, Is.EqualTo(MessageType.None));
            Assert.That(error, Is.EqualTo(ProtocolError.UnexpectedEnd));
        }

        private static int ReadInt32LittleEndian(byte[] bytes, int offset)
        {
            return bytes[offset] |
                bytes[offset + 1] << 8 |
                bytes[offset + 2] << 16 |
                bytes[offset + 3] << 24;
        }
    }
}
