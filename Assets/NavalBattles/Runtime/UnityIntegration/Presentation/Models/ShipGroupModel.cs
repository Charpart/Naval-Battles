namespace NavalBattles.Runtime.UnityIntegration.Presentation.Models
{
    public sealed class ShipGroupModel
    {
        public int length { get; }
        public int remainingCount { get; internal set; }

        public ShipGroupModel(int length)
        {
            this.length = length;
        }
    }
}
