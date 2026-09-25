namespace NavalBattles.Runtime.Domain.Boards
{
    public readonly struct ShotOutcome
    {
        public static readonly ShotOutcome Invalid = new ShotOutcome(ShotResult.Invalid, -1, 0);

        public ShotResult result { get; }

        public int shipIndex { get; }

        public int shipLength { get; }

        public ShotOutcome(ShotResult result, int shipIndex, int shipLength)
        {
            this.result = result;
            this.shipIndex = shipIndex;
            this.shipLength = shipLength;
        }
    }
}
