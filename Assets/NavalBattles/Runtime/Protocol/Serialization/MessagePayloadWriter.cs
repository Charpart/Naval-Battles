using System.Collections.Generic;
using System.IO;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Snapshots;

namespace NavalBattles.Runtime.Protocol.Serialization
{
    public static class MessagePayloadWriter
    {
        public static byte[] Write(ClientMessage message)
        {
            using var stream = new MemoryStream(32);
            using var writer = new BinaryWriter(stream);

            switch (message.type)
            {
                case MessageType.ResumeRequest:
                case MessageType.StateRequest:
                    writer.Write(message.knownRevision);
                    break;

                case MessageType.FireRequest:
                    writer.Write(message.commandId);
                    writer.Write(message.turnId);
                    writer.Write(message.cellIndex);
                    break;
            }

            return stream.ToArray();
        }

        public static byte[] Write(ServerMessage message)
        {
            using var stream = new MemoryStream(256);
            using var writer = new BinaryWriter(stream);

            switch (message.type)
            {
                case MessageType.SessionAccepted:
                    writer.Write((byte)message.player);
                    WriteDefinition(writer, message.definition);
                    WriteSnapshot(writer, message.snapshot);
                    break;

                case MessageType.FireResult:
                    WriteFireResult(writer, message);
                    break;

                case MessageType.StateSnapshot:
                    WriteSnapshot(writer, message.snapshot);
                    break;

                case MessageType.HeartbeatResponse:
                    writer.Write(message.serverTime);
                    break;

                case MessageType.RequestRejected:
                    writer.Write(message.commandId);
                    writer.Write((byte)message.rejectionReason);
                    break;
            }

            return stream.ToArray();
        }

        private static void WriteFireResult(BinaryWriter writer, ServerMessage message)
        {
            writer.Write(message.commandId);
            writer.Write((byte)message.requestStatus);
            writer.Write((byte)message.rejectionReason);
            writer.Write((byte)message.shotResult);
            writer.Write(message.sunkShipLength);
            WriteSnapshot(writer, message.snapshot);
        }

        private static void WriteDefinition(BinaryWriter writer, GameDefinition definition)
        {
            writer.Write(definition.width);
            writer.Write(definition.height);
            WriteIntCollection(writer, definition.shipLengths);
            writer.Write(definition.turnDurationSeconds);
        }

        private static void WriteSnapshot(BinaryWriter writer, PlayerSnapshot snapshot)
        {
            writer.Write(snapshot.revision);
            writer.Write(snapshot.turnId);
            writer.Write((byte)snapshot.player);
            writer.Write((byte)snapshot.activePlayer);
            writer.Write((byte)snapshot.winner);
            writer.Write(snapshot.turnDeadline);
            writer.Write(snapshot.lastProcessedCommandId);
            WriteSByteCollection(writer, snapshot.ownShipIndices);
            WriteEnumCollection(writer, snapshot.ownShots);
            WriteEnumCollection(writer, snapshot.opponentShots);
            WriteIntCollection(writer, snapshot.ownShipHitCounts);
            WriteBoolCollection(writer, snapshot.opponentShipsSunk);
        }

        private static void WriteIntCollection(BinaryWriter writer, IReadOnlyList<int> values)
        {
            writer.Write(values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                writer.Write(values[index]);
            }
        }

        private static void WriteSByteCollection(BinaryWriter writer, IReadOnlyList<sbyte> values)
        {
            writer.Write(values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                writer.Write(values[index]);
            }
        }

        private static void WriteEnumCollection(BinaryWriter writer, IReadOnlyList<NetworkShotState> values)
        {
            writer.Write(values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                writer.Write((byte)values[index]);
            }
        }

        private static void WriteBoolCollection(BinaryWriter writer, IReadOnlyList<bool> values)
        {
            writer.Write(values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                writer.Write(values[index]);
            }
        }
    }
}
