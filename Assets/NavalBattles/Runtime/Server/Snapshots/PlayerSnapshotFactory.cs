using NavalBattles.Runtime.Domain.Boards;
using NavalBattles.Runtime.Domain.Matches;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Snapshots;

namespace NavalBattles.Runtime.Server.Snapshots
{
    public static class PlayerSnapshotFactory
    {
        public static PlayerSnapshot Create(MatchState match, PlayerSlot player, ulong lastProcessedCommandId)
        {
            Board playerBoard = match.GetBoard(player);
            Board enemyBoard = match.GetBoard(PlayerSlotUtility.GetOpponent(player));

            return new PlayerSnapshot(
                match.revision,
                match.turnId,
                NetworkPlayerSlotMapper.Convert(player),
                NetworkPlayerSlotMapper.Convert(match.activePlayer),
                NetworkPlayerSlotMapper.Convert(match.winner),
                match.turnDeadline,
                lastProcessedCommandId,
                CreateShipIndices(playerBoard),
                CreateShots(playerBoard),
                CreateShots(enemyBoard),
                CreateShipHitCounts(playerBoard),
                CreateSunkStates(enemyBoard));
        }

        private static sbyte[] CreateShipIndices(Board board)
        {
            sbyte[] shipIndices = new sbyte[board.totalCellCount];
            for (int cellIndex = 0; cellIndex < shipIndices.Length; cellIndex++)
            {
                shipIndices[cellIndex] = (sbyte)board.GetShipIndex(cellIndex);
            }
            return shipIndices;
        }

        private static NetworkShotState[] CreateShots(Board board)
        {
            NetworkShotState[] shots = new NetworkShotState[board.totalCellCount];
            for (int cellIndex = 0; cellIndex < shots.Length; cellIndex++)
            {
                shots[cellIndex] = (NetworkShotState)board.GetCellShotState(cellIndex);
            }
            return shots;
        }

        private static int[] CreateShipHitCounts(Board board)
        {
            int[] hitCounts = new int[board.shipCount];
            for (int cellIndex = 0; cellIndex < board.totalCellCount; cellIndex++)
            {
                int shipIndex = board.GetShipIndex(cellIndex);

                if (shipIndex >= 0 && board.GetCellShotState(cellIndex) == CellShotState.Hit)
                {
                    hitCounts[shipIndex]++;
                }
            }
            return hitCounts;
        }

        private static bool[] CreateSunkStates(Board board)
        {
            bool[] sunkStates = new bool[board.shipCount];
            for (int shipIndex = 0; shipIndex < sunkStates.Length; shipIndex++)
            {
                sunkStates[shipIndex] = board.IsShipSunk(shipIndex);
            }
            return sunkStates;
        }
    }
}
