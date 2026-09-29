namespace NavalBattles.Runtime.UnityIntegration.Presentation.Models
{
    public enum PlayerGameStatus : byte
    {
        Waiting = 0,
        ActiveTurn = 1,
        WaitingTurn = 2,
        Winner = 3,
        Loser = 4,
        Disconnected = 5,
        Reconnecting = 6
    }
}
