using System;
using System.IO;
using NavalBattles.Runtime.Protocol.Messages;

namespace NavalBattles.Runtime.Protocol.Serialization
{
    public sealed class BinaryProtocolSerializer : IProtocolSerializer
    {
        private const byte PROTOCOL_VERSION = 1;
        private const int HEADER_LENGTH = 30;

        public byte[] Serialize(ClientMessage message)
        {
            byte[] body = MessagePayloadWriter.Write(message);

            return WritePacket(message.type, message.messageId, message.clientId, body);
        }

        public byte[] Serialize(ServerMessage message)
        {
            byte[] body = MessagePayloadWriter.Write(message);

            return WritePacket(message.type, message.messageId, Guid.Empty, body);
        }

        public bool TryDeserializeClient(
            ReadOnlyMemory<byte> payload,
            out ClientMessage message,
            out ProtocolError error)
        {
            message = default;

            if (TryReadPacket(payload, out ProtocolHeader header, out ReadOnlyMemory<byte> body, out error) == false)
            {
                return false;
            }

            if (IsClientMessage(header.type) == false)
            {
                error = ProtocolError.UnknownMessageType;

                return false;
            }

            var reader = new MessagePayloadReader(body);

            if (reader.TryReadClient(header, out message) == false)
            {
                error = reader.error;

                return false;
            }

            return TryFinishReading(reader, out error);
        }

        public bool TryDeserializeServer(
            ReadOnlyMemory<byte> payload,
            out ServerMessage message,
            out ProtocolError error)
        {
            message = default;

            if (TryReadPacket(payload, out ProtocolHeader header, out ReadOnlyMemory<byte> body, out error) == false)
            {
                return false;
            }

            if (IsServerMessage(header.type) == false)
            {
                error = ProtocolError.UnknownMessageType;

                return false;
            }

            var reader = new MessagePayloadReader(body);

            if (reader.TryReadServer(header, out message) == false)
            {
                error = reader.error;

                return false;
            }

            return TryFinishReading(reader, out error);
        }

        private static byte[] WritePacket(
            MessageType type,
            ulong messageId,
            Guid clientId,
            byte[] body)
        {
            using var stream = new MemoryStream(HEADER_LENGTH + body.Length);
            using var writer = new BinaryWriter(stream);
            writer.Write(PROTOCOL_VERSION);
            writer.Write((byte)type);
            writer.Write(messageId);
            writer.Write(clientId.ToByteArray());
            writer.Write(body.Length);
            writer.Write(body);

            return stream.ToArray();
        }

        private static bool TryReadPacket(
            ReadOnlyMemory<byte> packet,
            out ProtocolHeader header,
            out ReadOnlyMemory<byte> body,
            out ProtocolError error)
        {
            header = default;
            body = default;

            if (TryValidateHeader(packet, out error) == false ||
                TryReadBodyLength(packet, out int bodyLength, out error) == false)
            {
                return false;
            }

            ReadOnlySpan<byte> bytes = packet.Span;
            header = new ProtocolHeader(
                (MessageType)bytes[1],
                ReadUInt64LittleEndian(bytes, 2),
                new Guid(bytes.Slice(10, 16).ToArray()));
            body = packet.Slice(HEADER_LENGTH, bodyLength);
            error = ProtocolError.None;

            return true;
        }

        private static bool TryValidateHeader(
            ReadOnlyMemory<byte> packet,
            out ProtocolError error)
        {
            if (packet.Length < HEADER_LENGTH)
            {
                error = ProtocolError.UnexpectedEnd;

                return false;
            }

            error = packet.Span[0] == PROTOCOL_VERSION
                ? ProtocolError.None
                : ProtocolError.UnsupportedVersion;

            return error == ProtocolError.None;
        }

        private static bool TryReadBodyLength(
            ReadOnlyMemory<byte> packet,
            out int bodyLength,
            out ProtocolError error)
        {
            bodyLength = ReadInt32LittleEndian(packet.Span, 26);
            int availableBodyLength = packet.Length - HEADER_LENGTH;

            if (bodyLength > availableBodyLength)
            {
                error = ProtocolError.UnexpectedEnd;

                return false;
            }

            error = bodyLength < 0 || bodyLength != availableBodyLength
                ? ProtocolError.InvalidPayloadLength
                : ProtocolError.None;

            return error == ProtocolError.None;
        }

        private static bool TryFinishReading(MessagePayloadReader reader, out ProtocolError error)
        {
            if (reader.hasRemainingBytes)
            {
                error = ProtocolError.MalformedPayload;

                return false;
            }

            error = ProtocolError.None;

            return true;
        }

        private static bool IsClientMessage(MessageType type)
        {
            return type is >= MessageType.ConnectRequest and <= MessageType.HeartbeatRequest;
        }

        private static bool IsServerMessage(MessageType type)
        {
            return type is >= MessageType.SessionAccepted and <= MessageType.RequestRejected;
        }

        private static int ReadInt32LittleEndian(ReadOnlySpan<byte> bytes, int offset)
        {
            return bytes[offset] |
                bytes[offset + 1] << 8 |
                bytes[offset + 2] << 16 |
                bytes[offset + 3] << 24;
        }

        private static ulong ReadUInt64LittleEndian(ReadOnlySpan<byte> bytes, int offset)
        {
            ulong value = 0;

            for (int byteIndex = 0; byteIndex < sizeof(ulong); byteIndex++)
            {
                value |= (ulong)bytes[offset + byteIndex] << (byteIndex * 8);
            }

            return value;
        }
    }
}
