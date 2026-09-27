using System;
using NavalBattles.Runtime.Domain.Boards;
using NavalBattles.Runtime.Protocol.Messages;

namespace NavalBattles.Runtime.Server.Snapshots
{
    public static class NetworkShotMapper
    {
        public static NetworkShotState Convert(CellShotState state)
        {
            return state switch
            {
                CellShotState.None => NetworkShotState.None,
                CellShotState.Miss => NetworkShotState.Miss,
                CellShotState.Hit => NetworkShotState.Hit,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(state),
                    state,
                    "Unsupported cell shot state.")
            };
        }

        public static NetworkShotResult Convert(ShotResult result)
        {
            return result switch
            {
                ShotResult.Invalid => NetworkShotResult.Invalid,
                ShotResult.Miss => NetworkShotResult.Miss,
                ShotResult.Hit => NetworkShotResult.Hit,
                ShotResult.Sunk => NetworkShotResult.Sunk,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(result),
                    result,
                    "Unsupported shot result.")
            };
        }
    }
}
