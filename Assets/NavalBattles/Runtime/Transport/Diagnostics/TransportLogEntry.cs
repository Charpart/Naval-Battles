using System;

namespace NavalBattles.Runtime.Transport.Diagnostics
{
    public sealed class TransportLogEntry
    {
        public TransportMessageStatus status { get; }
        public TransportMessageDirection direction { get; }
        public TransportConnectionId connectionId { get; }
        public ReadOnlyMemory<byte> payload { get; }

        public int byteCount => payload.Length;
        public double time { get; }

        public TransportLogEntry(
            TransportMessageStatus status,
            TransportMessageDirection direction,
            TransportConnectionId connectionId,
            ReadOnlyMemory<byte> payload,
            double time)
        {
            this.status = status;
            this.direction = direction;
            this.connectionId = connectionId;
            this.payload = payload.ToArray();
            this.time = time;
        }
    }
}
