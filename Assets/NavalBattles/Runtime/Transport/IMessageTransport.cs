using System;

namespace NavalBattles.Runtime.Transport
{
    public interface IMessageTransport
    {
        event Action<TransportConnectionId, ReadOnlyMemory<byte>> OnReceived;

        void Send(TransportConnectionId target, ReadOnlyMemory<byte> payload);
    }
}
