using NavalBattles.Runtime.Transport.Diagnostics;

namespace NavalBattles.Runtime.Transport.Simulation
{
    public readonly struct ScheduledMessage
    {
        public TransportMessageDirection direction { get; }
        public TransportConnectionId connectionId { get; }

        public byte[] payload { get; }
        public double deliveryTime { get; }

        public ScheduledMessage(
            TransportMessageDirection direction,
            TransportConnectionId connectionId,
            byte[] payload,
            double deliveryTime)
        {
            this.direction = direction;
            this.connectionId = connectionId;
            this.payload = payload;
            this.deliveryTime = deliveryTime;
        }
    }
}
