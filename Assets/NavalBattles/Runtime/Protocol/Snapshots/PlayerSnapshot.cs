using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NavalBattles.Runtime.Protocol.Messages;

namespace NavalBattles.Runtime.Protocol.Snapshots
{
    public sealed class PlayerSnapshot
    {
        private readonly ReadOnlyCollection<sbyte> _ownShipIndices;
        private readonly ReadOnlyCollection<NetworkShotState> _ownShots;
        private readonly ReadOnlyCollection<NetworkShotState> _opponentShots;
        private readonly ReadOnlyCollection<int> _ownShipHitCounts;
        private readonly ReadOnlyCollection<bool> _opponentShipsSunk;

        public ulong revision { get; }
        public ulong turnId { get; }
        public NetworkPlayerSlot player { get; }
        public NetworkPlayerSlot activePlayer { get; }
        public NetworkPlayerSlot winner { get; }
        public double turnDeadline { get; }
        public ulong lastProcessedCommandId { get; }

        public IReadOnlyList<sbyte> ownShipIndices => _ownShipIndices;
        public IReadOnlyList<NetworkShotState> ownShots => _ownShots;
        public IReadOnlyList<NetworkShotState> opponentShots => _opponentShots;
        public IReadOnlyList<int> ownShipHitCounts => _ownShipHitCounts;
        public IReadOnlyList<bool> opponentShipsSunk => _opponentShipsSunk;

        public PlayerSnapshot(
            ulong revision,
            ulong turnId,
            NetworkPlayerSlot player,
            NetworkPlayerSlot activePlayer,
            NetworkPlayerSlot winner,
            double turnDeadline,
            ulong lastProcessedCommandId,
            sbyte[] ownShipIndices,
            NetworkShotState[] ownShots,
            NetworkShotState[] opponentShots,
            int[] ownShipHitCounts,
            bool[] opponentShipsSunk)
        {
            this.revision = revision;
            this.turnId = turnId;
            this.player = player;
            this.activePlayer = activePlayer;
            this.winner = winner;
            this.turnDeadline = turnDeadline;
            this.lastProcessedCommandId = lastProcessedCommandId;
            _ownShipIndices = Array.AsReadOnly((sbyte[])ownShipIndices.Clone());
            _ownShots = Array.AsReadOnly((NetworkShotState[])ownShots.Clone());
            _opponentShots = Array.AsReadOnly((NetworkShotState[])opponentShots.Clone());
            _ownShipHitCounts = Array.AsReadOnly((int[])ownShipHitCounts.Clone());
            _opponentShipsSunk = Array.AsReadOnly((bool[])opponentShipsSunk.Clone());
        }
    }
}
