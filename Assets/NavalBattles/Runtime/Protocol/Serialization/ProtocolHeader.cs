using System;
using NavalBattles.Runtime.Protocol.Messages;

namespace NavalBattles.Runtime.Protocol.Serialization
{
    public readonly struct ProtocolHeader
    {
        public MessageType type { get; }
        public ulong messageId { get; }
        public Guid clientId { get; }

        public ProtocolHeader(MessageType type, ulong messageId, Guid clientId)
        {
            this.type = type;
            this.messageId = messageId;
            this.clientId = clientId;
        }
    }
}
