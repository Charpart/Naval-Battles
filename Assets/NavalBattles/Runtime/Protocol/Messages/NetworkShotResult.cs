namespace NavalBattles.Runtime.Protocol.Messages
{
    public enum NetworkShotResult : byte
    {
        Invalid = 0,
        Miss = 1,
        Hit = 2,
        Sunk = 3
    }
}
