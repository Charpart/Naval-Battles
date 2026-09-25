namespace NavalBattles.Runtime.Protocol.Messages
{
    public enum MessageType : byte
    {
        None = 0,
        ConnectRequest = 1,
        ResumeRequest = 2,
        FireRequest = 3,
        StateRequest = 4,
        HeartbeatRequest = 5,
        SessionAccepted = 101,
        FireResult = 102,
        StateSnapshot = 103,
        HeartbeatResponse = 104,
        RequestRejected = 105
    }
}
