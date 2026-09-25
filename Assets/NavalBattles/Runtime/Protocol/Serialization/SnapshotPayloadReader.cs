using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Snapshots;

namespace NavalBattles.Runtime.Protocol.Serialization
{
    public sealed class SnapshotPayloadReader
    {
        private readonly LittleEndianReader _reader;

        public SnapshotPayloadReader(LittleEndianReader reader)
        {
            _reader = reader;
        }

        public bool TryReadDefinition(out GameDefinition definition)
        {
            definition = null;

            if (_reader.TryReadInt32(out int width) == false ||
                _reader.TryReadInt32(out int height) == false ||
                _reader.TryReadIntArray(out int[] shipLengths) == false ||
                _reader.TryReadDouble(out double turnDurationSeconds) == false)
            {
                return false;
            }

            definition = new GameDefinition(width, height, shipLengths, turnDurationSeconds);

            return true;
        }

        public bool TryReadSnapshot(out PlayerSnapshot snapshot)
        {
            snapshot = null;

            if (TryReadSnapshotHeader(out SnapshotHeader header) == false ||
                TryReadSnapshotCollections(out SnapshotCollections collections) == false)
            {
                return false;
            }

            snapshot = new PlayerSnapshot(
                header.revision,
                header.turnId,
                header.player,
                header.activePlayer,
                header.winner,
                header.turnDeadline,
                header.lastProcessedCommandId,
                collections.ownShipIndices,
                collections.ownShots,
                collections.opponentShots,
                collections.ownShipHitCounts,
                collections.opponentShipsSunk);

            return true;
        }

        private bool TryReadSnapshotHeader(out SnapshotHeader header)
        {
            header = default;

            if (_reader.TryReadUInt64(out ulong revision) == false ||
                _reader.TryReadUInt64(out ulong turnId) == false ||
                _reader.TryReadByte(out byte player) == false ||
                _reader.TryReadByte(out byte activePlayer) == false ||
                _reader.TryReadByte(out byte winner) == false ||
                _reader.TryReadDouble(out double turnDeadline) == false ||
                _reader.TryReadUInt64(out ulong commandId) == false)
            {
                return false;
            }

            header = new SnapshotHeader(
                revision,
                turnId,
                (NetworkPlayerSlot)player,
                (NetworkPlayerSlot)activePlayer,
                (NetworkPlayerSlot)winner,
                turnDeadline,
                commandId);

            return true;
        }

        private bool TryReadSnapshotCollections(out SnapshotCollections collections)
        {
            collections = default;

            if (_reader.TryReadSByteArray(out sbyte[] ownShipIndices) == false ||
                _reader.TryReadShotStateArray(out NetworkShotState[] ownShots) == false ||
                _reader.TryReadShotStateArray(out NetworkShotState[] opponentShots) == false ||
                _reader.TryReadIntArray(out int[] ownShipHitCounts) == false ||
                _reader.TryReadBoolArray(out bool[] opponentShipsSunk) == false)
            {
                return false;
            }

            collections = new SnapshotCollections(
                ownShipIndices,
                ownShots,
                opponentShots,
                ownShipHitCounts,
                opponentShipsSunk);

            return true;
        }

        private readonly struct SnapshotHeader
        {
            public readonly ulong revision;
            public readonly ulong turnId;
            public readonly NetworkPlayerSlot player;
            public readonly NetworkPlayerSlot activePlayer;
            public readonly NetworkPlayerSlot winner;
            public readonly double turnDeadline;
            public readonly ulong lastProcessedCommandId;

            public SnapshotHeader(
                ulong revision,
                ulong turnId,
                NetworkPlayerSlot player,
                NetworkPlayerSlot activePlayer,
                NetworkPlayerSlot winner,
                double turnDeadline,
                ulong lastProcessedCommandId)
            {
                this.revision = revision;
                this.turnId = turnId;
                this.player = player;
                this.activePlayer = activePlayer;
                this.winner = winner;
                this.turnDeadline = turnDeadline;
                this.lastProcessedCommandId = lastProcessedCommandId;
            }
        }

        private readonly struct SnapshotCollections
        {
            public readonly sbyte[] ownShipIndices;
            public readonly NetworkShotState[] ownShots;
            public readonly NetworkShotState[] opponentShots;
            public readonly int[] ownShipHitCounts;
            public readonly bool[] opponentShipsSunk;

            public SnapshotCollections(
                sbyte[] ownShipIndices,
                NetworkShotState[] ownShots,
                NetworkShotState[] opponentShots,
                int[] ownShipHitCounts,
                bool[] opponentShipsSunk)
            {
                this.ownShipIndices = ownShipIndices;
                this.ownShots = ownShots;
                this.opponentShots = opponentShots;
                this.ownShipHitCounts = ownShipHitCounts;
                this.opponentShipsSunk = opponentShipsSunk;
            }
        }
    }
}
