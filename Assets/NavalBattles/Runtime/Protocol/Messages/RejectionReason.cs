namespace NavalBattles.Runtime.Protocol.Messages
{
    public enum RejectionReason : byte
    {
        None = 0,
        ServerFull = 1,
        UnknownSession = 2,
        WrongTurn = 3,
        StaleTurn = 4,
        TurnExpired = 5,
        InvalidTarget = 6,
        MatchFinished = 7,
        InvalidMessage = 8
    }
}
