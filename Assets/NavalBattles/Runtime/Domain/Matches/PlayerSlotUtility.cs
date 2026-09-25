namespace NavalBattles.Runtime.Domain.Matches
{
    public static class PlayerSlotUtility
    {
        public static PlayerSlot GetOpponent(PlayerSlot player)
        {
            return player switch
            {
                PlayerSlot.First => PlayerSlot.Second,
                PlayerSlot.Second => PlayerSlot.First,
                _ => PlayerSlot.None
            };
        }
    }
}
