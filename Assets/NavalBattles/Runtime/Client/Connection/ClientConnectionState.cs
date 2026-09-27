namespace NavalBattles.Runtime.Client.Connection
{
    public enum ClientConnectionState : byte
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Reconnecting = 3
    }
}
