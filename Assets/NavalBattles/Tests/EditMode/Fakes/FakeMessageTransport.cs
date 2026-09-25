using System;
using System.Collections.Generic;
using NavalBattles.Runtime.Transport;

namespace NavalBattles.Tests.EditMode.Fakes
{
    public sealed class FakeMessageTransport : IMessageTransport
    {
        public event Action<TransportConnectionId, ReadOnlyMemory<byte>> OnReceived;

        private readonly List<SentMessage> _sentMessages = new List<SentMessage>();

        public IReadOnlyList<SentMessage> sentMessages => _sentMessages;

        public void Send(TransportConnectionId target, ReadOnlyMemory<byte> payload)
        {
            _sentMessages.Add(new SentMessage(target, payload.ToArray()));
        }

        public void Deliver(TransportConnectionId source, byte[] payload)
        {
            OnReceived?.Invoke(source, (byte[])payload.Clone());
        }

        public void Clear()
        {
            _sentMessages.Clear();
        }
    }

    public class SentMessage
    {
        public TransportConnectionId target { get; }
        public byte[] payload { get; }

        public SentMessage(TransportConnectionId target, byte[] payload)
        {
            this.target = target;
            this.payload = payload;
        }
    }
}
