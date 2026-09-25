namespace NavalBattles.Runtime.Domain.Configuration
{
    public enum GameRulesValidationError : byte
    {
        None = 0,
        InvalidBoardSize = 1,
        EmptyFleet = 2,
        InvalidShipLength = 3,
        FleetDoesNotFit = 4,
        InvalidTurnDuration = 5
    }
}
