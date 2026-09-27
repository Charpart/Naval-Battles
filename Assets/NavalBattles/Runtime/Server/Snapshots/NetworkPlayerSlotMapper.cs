using System;
using NavalBattles.Runtime.Domain.Matches;
using NavalBattles.Runtime.Protocol.Messages;

namespace NavalBattles.Runtime.Server.Snapshots
{
    public static class NetworkPlayerSlotMapper
    {
        public static NetworkPlayerSlot Convert(PlayerSlot player)
        {
            return player switch
            {
                PlayerSlot.None => NetworkPlayerSlot.None,
                PlayerSlot.First => NetworkPlayerSlot.First,
                PlayerSlot.Second => NetworkPlayerSlot.Second,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(player),
                    player,
                    "Unsupported player slot.")
            };
        }
    }
}
