namespace NavalBattles.Runtime.Transport.Diagnostics
{
    public enum TransportMessageStatus : byte
    {
        Sent = 0,
        Received = 1,
        Dropped = 2,
        Duplicated = 3
    }
}
