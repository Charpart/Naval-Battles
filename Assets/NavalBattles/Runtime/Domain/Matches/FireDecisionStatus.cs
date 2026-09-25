namespace NavalBattles.Runtime.Domain.Matches
{
    public enum FireDecisionStatus : byte
    {
        None = 0,
        Accepted = 1,
        MatchFinished = 2,
        StaleTurn = 3,
        WrongTurn = 4,
        TurnExpired = 5,
        InvalidTarget = 6
    }
}
