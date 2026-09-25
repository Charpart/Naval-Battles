using System;

namespace NavalBattles.Runtime.Transport
{
    public readonly struct TransportConnectionId : IEquatable<TransportConnectionId>
    {
        public int value { get; }

        public TransportConnectionId(int value)
        {
            this.value = value;
        }

        public bool Equals(TransportConnectionId other)
        {
            return value == other.value;
        }

        public override bool Equals(object obj)
        {
            return obj is TransportConnectionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value;
        }

        public static bool operator ==(TransportConnectionId left, TransportConnectionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransportConnectionId left, TransportConnectionId right)
        {
            return left.Equals(right) == false;
        }
    }
}
