namespace NavalBattles.Runtime.Domain.Fleets
{
    public readonly struct ShipState
    {
        public int length { get; }
        public int receivedHitCount { get; }

        public bool isSunk => receivedHitCount >= length;

        public ShipState(int length, int receivedHitCount)
        {
            this.length = length;
            this.receivedHitCount = receivedHitCount;
        }

        public ShipState RegisterHit()
        {
            int updatedHitCount = receivedHitCount + 1;
            return new ShipState(length, updatedHitCount);
        }
    }
}
