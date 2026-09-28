using System;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Snapshots;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Models
{
    public static class BoardPresentationWriter
    {
        public static void FillOwnerBoard(BoardPanelModel target, PlayerSnapshot snapshot)
        {
            ValidateOwner(target, snapshot);

            for (int cellIndex = 0; cellIndex < target.cells.Length; cellIndex++)
            {
                int shipIndex = snapshot.ownShipIndices[cellIndex];
                ValidateShipIndex(target, shipIndex);
                target.cells[cellIndex] = GetOwnerCellState(target, snapshot, cellIndex, shipIndex);
            }

            FillOwnerFleet(target, snapshot);
        }

        public static void FillOpponentBoard(BoardPanelModel target, PlayerSnapshot snapshot)
        {
            ValidateOpponent(target, snapshot);

            for (int cellIndex = 0; cellIndex < target.cells.Length; cellIndex++)
            {
                target.cells[cellIndex] = snapshot.opponentShots[cellIndex] switch
                {
                    NetworkShotState.None => CellVisualState.Unknown,
                    NetworkShotState.Miss => CellVisualState.Miss,
                    NetworkShotState.Hit => CellVisualState.Hit,
                    _ => throw new ArgumentOutOfRangeException(nameof(snapshot))
                };
            }

            FillOpponentFleet(target, snapshot);
        }

        private static CellVisualState GetOwnerCellState(
            BoardPanelModel target,
            PlayerSnapshot snapshot,
            int cellIndex,
            int shipIndex)
        {
            if (shipIndex >= 0 && IsOwnerShipSunk(target, snapshot, shipIndex))
                return CellVisualState.Sunk;

            return snapshot.ownShots[cellIndex] switch
            {
                NetworkShotState.Miss => CellVisualState.Miss,
                NetworkShotState.Hit => CellVisualState.Hit,
                NetworkShotState.None when shipIndex >= 0 => CellVisualState.Ship,
                NetworkShotState.None => CellVisualState.Water,
                _ => throw new ArgumentOutOfRangeException(nameof(snapshot))
            };
        }

        private static void FillOwnerFleet(BoardPanelModel target, PlayerSnapshot snapshot)
        {
            ClearGroups(target.shipGroups);
            for (int shipIndex = 0; shipIndex < target.shipCount; shipIndex++)
            {
                if (IsOwnerShipSunk(target, snapshot, shipIndex) == false)
                    IncrementGroup(target, target.GetShipLength(shipIndex));
            }
        }

        private static void FillOpponentFleet(BoardPanelModel target, PlayerSnapshot snapshot)
        {
            ClearGroups(target.shipGroups);

            for (int shipIndex = 0; shipIndex < target.shipCount; shipIndex++)
            {
                if (snapshot.opponentShipsSunk[shipIndex] == false)
                    IncrementGroup(target, target.GetShipLength(shipIndex));
            }
        }

        private static bool IsOwnerShipSunk(
            BoardPanelModel target,
            PlayerSnapshot snapshot,
            int shipIndex)
        {
            return snapshot.ownShipHitCounts[shipIndex] >= target.GetShipLength(shipIndex);
        }

        private static void ClearGroups(ShipGroupModel[] groups)
        {
            for (int index = 0; index < groups.Length; index++)
            {
                groups[index].remainingCount = 0;
            }
        }

        private static void IncrementGroup(BoardPanelModel target, int shipLength)
        {
            for (int index = 0; index < target.shipGroups.Length; index++)
            {
                if (target.shipGroups[index].length == shipLength)
                {
                    target.shipGroups[index].remainingCount++;
                    return;
                }
            }

            throw new ArgumentException("Ship length has no presentation group.", nameof(target));
        }

        private static void ValidateOwner(BoardPanelModel target, PlayerSnapshot snapshot)
        {
            ValidateCommon(target, snapshot);

            if (snapshot.ownShipIndices.Count != target.cells.Length ||
                snapshot.ownShots.Count != target.cells.Length ||
                snapshot.ownShipHitCounts.Count != target.shipCount)
            {
                throw new ArgumentException("Owner snapshot does not match the board definition.", nameof(snapshot));
            }
        }

        private static void ValidateOpponent(BoardPanelModel target, PlayerSnapshot snapshot)
        {
            ValidateCommon(target, snapshot);

            if (snapshot.opponentShots.Count != target.cells.Length ||
                snapshot.opponentShipsSunk.Count != target.shipCount)
            {
                throw new ArgumentException("Opponent snapshot does not match the board definition.", nameof(snapshot));
            }
        }

        private static void ValidateCommon(BoardPanelModel target, PlayerSnapshot snapshot)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
        }

        private static void ValidateShipIndex(BoardPanelModel target, int shipIndex)
        {
            if (shipIndex < -1 || shipIndex >= target.shipCount)
                throw new ArgumentException("Snapshot contains an invalid ship index.", nameof(target));
        }
    }
}
